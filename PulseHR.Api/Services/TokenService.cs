using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PulseHR.Api.Models;

namespace PulseHR.Api.Services;

public interface ITokenService
{
    string GenerateAccessToken(Employee employee);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    DateTime AccessTokenExpiry { get; }
    DateTime RefreshTokenExpiry { get; }
}

public class TokenService(IConfiguration config) : ITokenService
{
    private readonly string _key = config["Jwt:Key"]
        ?? throw new InvalidOperationException("JWT Key is not configured.");
    private readonly string _issuer   = config["Jwt:Issuer"]   ?? "PulseHR.Api";
    private readonly string _audience = config["Jwt:Audience"] ?? "PulseHR.Client";
    private readonly int _accessMins  = int.Parse(config["Jwt:AccessTokenExpiryMinutes"] ?? "15");
    private readonly int _refreshDays = int.Parse(config["Jwt:RefreshTokenExpiryDays"]  ?? "7");

    public DateTime AccessTokenExpiry  => DateTime.UtcNow.AddMinutes(_accessMins);
    public DateTime RefreshTokenExpiry => DateTime.UtcNow.AddDays(_refreshDays);

    public string GenerateAccessToken(Employee employee)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   employee.EmployeeId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, employee.Email),
            new Claim("name",     employee.Name),
            new Claim("isAdmin",  employee.IsAdmin.ToString().ToLower()),
            new Claim("userType", employee.UserType),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var expiry = AccessTokenExpiry;
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiry,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var validation = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
            ValidateIssuer           = true,
            ValidIssuer              = _issuer,
            ValidateAudience         = true,
            ValidAudience            = _audience,
            ValidateLifetime         = false  // allow expired tokens for refresh
        };

        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, validation, out var securityToken);

            if (securityToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
