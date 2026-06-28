using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PulseHR.Api.DTOs.Auth;
using PulseHR.Api.DTOs.Common;
using PulseHR.Api.Services;

namespace PulseHR.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("AuthLimit")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private string IpAddress =>
        Request.Headers.TryGetValue("X-Forwarded-For", out var fwd)
            ? fwd.ToString().Split(',')[0].Trim()
            : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // POST api/auth/employee/login
    [HttpPost("employee/login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> EmployeeLogin([FromBody] LoginRequest req)
    {
        var result = await authService.LoginAsync(req.Email, req.Password, requireAdmin: false, IpAddress);
        if (result is null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid email or password."));

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponse<LoginResponse>.Ok(result));
    }

    // POST api/auth/admin/login
    [HttpPost("admin/login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> AdminLogin([FromBody] LoginRequest req)
    {
        var result = await authService.LoginAsync(req.Email, req.Password, requireAdmin: true, IpAddress);
        if (result is null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid credentials or insufficient privileges."));

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponse<LoginResponse>.Ok(result));
    }

    // POST api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
    {
        var token = req.RefreshToken
            ?? Request.Cookies["refreshToken"]
            ?? string.Empty;

        await authService.LogoutAsync(token, IpAddress);
        Response.Cookies.Delete("refreshToken");
        return Ok(ApiResponse<object>.SuccessMessage("Logged out successfully."));
    }

    // POST api/auth/refresh
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<RefreshResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var token = req.RefreshToken
            ?? Request.Cookies["refreshToken"]
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized(ApiResponse<object>.Fail("Refresh token is required."));

        var result = await authService.RefreshAsync(token, IpAddress);
        if (result is null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid or expired refresh token."));

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponse<RefreshResponse>.Ok(result));
    }

    // GET api/auth/me
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<MeResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Me()
    {
        var employeeId = GetEmployeeId();
        if (employeeId is null) return Unauthorized();

        var result = await authService.GetMeAsync(employeeId.Value);
        if (result is null) return Unauthorized();

        return Ok(ApiResponse<MeResponse>.Ok(result));
    }

    // POST api/auth/change-password
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var employeeId = GetEmployeeId();
        if (employeeId is null) return Unauthorized();

        await authService.ChangePasswordAsync(employeeId.Value, req.CurrentPassword, req.NewPassword);
        return Ok(ApiResponse<object>.SuccessMessage("Password changed successfully."));
    }

    private void SetRefreshTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Requires HTTPS (standard for prod API)
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }

    private int? GetEmployeeId()
    {
        var claim = User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
