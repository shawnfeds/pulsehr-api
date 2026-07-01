using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PulseHR.Api.Data;
using PulseHR.Api.Middleware;
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

app.Run();
