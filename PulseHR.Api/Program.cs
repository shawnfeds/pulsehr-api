using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PulseHR.Api.Data;
using PulseHR.Api.Middleware;
using PulseHR.Api.Models;
using PulseHR.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// ── Database ──────────────────────────────────────────────
builder.Services.AddDbContext<PulseHRContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Authentication ────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key is not configured in appsettings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.FromSeconds(30)
        };
    });

// ── Authorization Policies ────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("isAdmin", "true"));
});

// ── Rate Limiting ─────────────────────────────────────────
var rl = builder.Configuration.GetSection("RateLimiting");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Auth endpoints: IP-keyed, 10 req/min
    options.AddSlidingWindowLimiter("AuthLimit", limiter =>
    {
        limiter.Window           = TimeSpan.FromSeconds(rl.GetValue<int>("AuthWindowSeconds", 60));
        limiter.PermitLimit      = rl.GetValue<int>("AuthPermitLimit", 10);
        limiter.SegmentsPerWindow = 6;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit       = 0;
    });

    // Upload endpoints: IP-keyed, 10 req/min
    options.AddSlidingWindowLimiter("UploadLimit", limiter =>
    {
        limiter.Window            = TimeSpan.FromSeconds(rl.GetValue<int>("UploadWindowSeconds", 60));
        limiter.PermitLimit       = rl.GetValue<int>("UploadPermitLimit", 10);
        limiter.SegmentsPerWindow = 6;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit        = 0;
    });

    // Global policy: user-keyed (falls back to IP), 100 req/min
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User?.FindFirst("sub")?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(userId, _ => new SlidingWindowRateLimiterOptions
        {
            Window            = TimeSpan.FromSeconds(rl.GetValue<int>("GlobalWindowSeconds", 60)),
            PermitLimit       = rl.GetValue<int>("GlobalPermitLimit", 100),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit        = 0
        });
    });

    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
        ctx.HttpContext.Response.ContentType = "application/json";
        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            ctx.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString("0");
        await ctx.HttpContext.Response.WriteAsync(
            "{\"success\":false,\"message\":\"Too many requests. Please try again later.\"}", token);
    };
});

// ── CORS ──────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5500"];

builder.Services.AddCors(options =>
    options.AddPolicy("PulseHRCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ── Services ──────────────────────────────────────────────
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// ── Controllers ───────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ── Response Compression ──────────────────────────────────
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// ── Health Checks ─────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PulseHRContext>("database");

// ── Swagger ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title       = "PulseHR API",
        Version     = "v1",
        Description = "Production-ready HR management API"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description  = "Enter your JWT token (without 'Bearer ' prefix)."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────

// ── Middleware Pipeline ───────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();

// Swagger available in all environments (for testing hosted API)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PulseHR API v1");
    c.RoutePrefix = "swagger";
});

// Skip HTTPS redirect in production — Render handles SSL at the proxy level
if (!app.Environment.IsProduction())
    app.UseHttpsRedirection();

app.UseResponseCompression();
app.UseStaticFiles();
app.UseCors("PulseHRCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint (no auth required)
app.MapHealthChecks("/health").AllowAnonymous();

app.MapControllers();

// ── Automatic Database Creation & Seeding ──────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PulseHRContext>();
    try
    {
        // Automatically create PostgreSQL tables if they don't exist
        context.Database.EnsureCreated();

        // Seed Roles
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Role { Name = "Admin", Description = "System administrator with full access" },
                new Role { Name = "Employee", Description = "Standard employee with self-service access" }
            );
            context.SaveChanges();
        }

        var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
        var employeeRole = context.Roles.FirstOrDefault(r => r.Name == "Employee");

        // Seed Default Admin User
        if (!context.Employees.Any(e => e.Email == "admin@pulsehr.com"))
        {
            var admin = new Employee
            {
                Name = "Administrator",
                Email = "admin@pulsehr.com",
                PasswordHash = "$2a$11$qR3mO528r20Jm6o2yLdWeeM0wBvHlhjD4QvA6B97R9xK6a8gL5xUq", // AdminPassword123!
                PasswordSalt = string.Empty,
                IsAdmin = true,
                UserType = "Admin",
                Role = "System Admin",
                Dept = "IT",
                Status = "active",
                JoinDate = new DateOnly(2026, 1, 1),
                Phone = "+1 555 0199",
                Location = "New York, NY",
                AvatarText = "AD",
                AvatarColor = "#dc2626",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Employees.Add(admin);
            context.SaveChanges();

            if (adminRole != null)
            {
                context.EmployeeRoles.Add(new EmployeeRole { EmployeeId = admin.EmployeeId, RoleId = adminRole.RoleId });
            }

            context.LeaveBalances.AddRange(
                new LeaveBalance { EmployeeId = admin.EmployeeId, LeaveType = "Sick", Total = 12, Used = 0 },
                new LeaveBalance { EmployeeId = admin.EmployeeId, LeaveType = "Casual", Total = 12, Used = 0 }
            );
            context.SaveChanges();
        }

        // Seed Default Employee User
        if (!context.Employees.Any(e => e.Email == "priya@pulsehr.com"))
        {
            var employee = new Employee
            {
                Name = "Priya Sharma",
                Email = "priya@pulsehr.com",
                PasswordHash = "$2a$11$0F/oM5eJswK9c64Jt3bO7O.lC5eNq5D1jV9aVf6Zk9XyS7t5T.Z6i", // EmployeePassword123!
                PasswordSalt = string.Empty,
                IsAdmin = false,
                UserType = "Employee",
                Role = "Software Engineer",
                Dept = "Engineering",
                Status = "active",
                JoinDate = new DateOnly(2026, 2, 15),
                Phone = "+91 98765 43210",
                Location = "Bengaluru, KA",
                AvatarText = "PS",
                AvatarColor = "#7c3aed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Employees.Add(employee);
            context.SaveChanges();

            if (employeeRole != null)
            {
                context.EmployeeRoles.Add(new EmployeeRole { EmployeeId = employee.EmployeeId, RoleId = employeeRole.RoleId });
            }

            context.LeaveBalances.AddRange(
                new LeaveBalance { EmployeeId = employee.EmployeeId, LeaveType = "Sick", Total = 12, Used = 0 },
                new LeaveBalance { EmployeeId = employee.EmployeeId, LeaveType = "Casual", Total = 12, Used = 0 }
            );
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
