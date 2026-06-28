using System.ComponentModel.DataAnnotations;

namespace PulseHR.Api.DTOs.Auth;

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Dept { get; set; }
    public string? Avatar { get; set; }
    public string? AvatarColor { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? JoinDate { get; set; }
    public List<int> Projects { get; set; } = [];
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsAdmin { get; set; }
    public string UserType { get; set; } = string.Empty;
}

public class RefreshRequest
{
    public string? RefreshToken { get; set; }
}

public class RefreshResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class MeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Dept { get; set; }
    public string? Avatar { get; set; }
    public string? AvatarColor { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? JoinDate { get; set; }
    public List<int> Projects { get; set; } = [];
    public bool IsAdmin { get; set; }
    public string UserType { get; set; } = string.Empty;
}
