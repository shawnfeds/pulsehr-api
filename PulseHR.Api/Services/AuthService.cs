using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Data;
using PulseHR.Api.DTOs.Auth;
using PulseHR.Api.Models;

namespace PulseHR.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string email, string password, bool requireAdmin, string ipAddress);
    Task<RefreshResponse?> RefreshAsync(string refreshToken, string ipAddress);
    Task LogoutAsync(string refreshToken, string ipAddress);
    Task ChangePasswordAsync(int employeeId, string currentPassword, string newPassword);
    Task<MeResponse?> GetMeAsync(int employeeId);
}

public class AuthService(PulseHRContext db, ITokenService tokenService) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(string email, string password, bool requireAdmin, string ipAddress)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Email == email && e.Status == "active");

        if (employee is null) return null;
        if (requireAdmin && !employee.IsAdmin) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash)) return null;

        var accessToken   = tokenService.GenerateAccessToken(employee);
        var rawRefresh    = tokenService.GenerateRefreshToken();
        var refreshExpiry = tokenService.RefreshTokenExpiry;
        var accessExpiry  = tokenService.AccessTokenExpiry;

        // Revoke old tokens for this employee (optional: keep only last N)
        var oldTokens = await db.RefreshTokens
            .Where(t => t.EmployeeId == employee.EmployeeId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var old in oldTokens)
        {
            old.RevokedAt      = DateTime.UtcNow;
            old.RevokedByIp    = ipAddress;
            old.ReplacedByToken = rawRefresh;
        }

        db.RefreshTokens.Add(new RefreshToken
        {
            EmployeeId   = employee.EmployeeId,
            Token        = rawRefresh,
            ExpiresAt    = refreshExpiry,
            CreatedAt    = DateTime.UtcNow,
            CreatedByIp  = ipAddress
        });

        await db.SaveChangesAsync();

        var projectIds = await db.ProjectMembers
            .Where(pm => pm.EmployeeId == employee.EmployeeId)
            .Select(pm => pm.ProjectId)
            .ToListAsync();

        return new LoginResponse
        {
            Id           = employee.EmployeeId,
            Name         = employee.Name,
            Email        = employee.Email,
            Role         = employee.Role,
            Dept         = employee.Dept,
            Avatar       = employee.AvatarText,
            AvatarColor  = employee.AvatarColor,
            AvatarUrl    = employee.AvatarUrl,
            Phone        = employee.Phone,
            Location     = employee.Location,
            JoinDate     = employee.JoinDate?.ToString("yyyy-MM-dd"),
            Projects     = projectIds,
            Token        = accessToken,
            RefreshToken = rawRefresh,
            ExpiresAt    = accessExpiry,
            IsAdmin      = employee.IsAdmin,
            UserType     = employee.UserType
        };
    }

    public async Task<RefreshResponse?> RefreshAsync(string refreshToken, string ipAddress)
    {
        var stored = await db.RefreshTokens
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (stored is null || stored.Employee is null) return null;

        // Detect reuse of a revoked token — revoke entire family
        if (stored.RevokedAt is not null)
        {
            await RevokeDescendantsAsync(stored, ipAddress, "Reuse detected");
            await db.SaveChangesAsync();
            return null;
        }

        if (stored.ExpiresAt < DateTime.UtcNow) return null;

        var newRawRefresh = tokenService.GenerateRefreshToken();
        var newAccessToken = tokenService.GenerateAccessToken(stored.Employee);
        var accessExpiry  = tokenService.AccessTokenExpiry;
        var refreshExpiry = tokenService.RefreshTokenExpiry;

        // Rotate: revoke current, issue new
        stored.RevokedAt       = DateTime.UtcNow;
        stored.RevokedByIp     = ipAddress;
        stored.ReplacedByToken = newRawRefresh;

        db.RefreshTokens.Add(new RefreshToken
        {
            EmployeeId  = stored.EmployeeId,
            Token       = newRawRefresh,
            ExpiresAt   = refreshExpiry,
            CreatedAt   = DateTime.UtcNow,
            CreatedByIp = ipAddress
        });

        await db.SaveChangesAsync();

        return new RefreshResponse
        {
            Token        = newAccessToken,
            RefreshToken = newRawRefresh,
            ExpiresAt    = accessExpiry
        };
    }

    public async Task LogoutAsync(string refreshToken, string ipAddress)
    {
        var stored = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken && t.RevokedAt == null);

        if (stored is null) return;

        stored.RevokedAt   = DateTime.UtcNow;
        stored.RevokedByIp = ipAddress;
        await db.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(int employeeId, string currentPassword, string newPassword)
    {
        var employee = await db.Employees.FindAsync(employeeId)
            ?? throw new KeyNotFoundException("Employee not found.");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, employee.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        employee.UpdatedAt    = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<MeResponse?> GetMeAsync(int employeeId)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee is null) return null;

        var projectIds = await db.ProjectMembers
            .Where(pm => pm.EmployeeId == employeeId)
            .Select(pm => pm.ProjectId)
            .ToListAsync();

        return new MeResponse
        {
            Id          = employee.EmployeeId,
            Name        = employee.Name,
            Email       = employee.Email,
            Role        = employee.Role,
            Dept        = employee.Dept,
            Avatar      = employee.AvatarText,
            AvatarColor = employee.AvatarColor,
            AvatarUrl   = employee.AvatarUrl,
            Phone       = employee.Phone,
            Location    = employee.Location,
            JoinDate    = employee.JoinDate?.ToString("yyyy-MM-dd"),
            Projects    = projectIds,
            IsAdmin     = employee.IsAdmin,
            UserType    = employee.UserType
        };
    }

    private async Task RevokeDescendantsAsync(RefreshToken token, string ipAddress, string reason)
    {
        if (token.ReplacedByToken is null) return;

        var child = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token.ReplacedByToken);

        if (child is not null)
        {
            child.RevokedAt   = DateTime.UtcNow;
            child.RevokedByIp = ipAddress;
            await RevokeDescendantsAsync(child, ipAddress, reason);
        }
    }
}
