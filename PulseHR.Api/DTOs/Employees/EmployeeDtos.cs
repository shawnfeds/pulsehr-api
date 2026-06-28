using System.ComponentModel.DataAnnotations;

namespace PulseHR.Api.DTOs.Employees;

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Dept { get; set; }
    public string Status { get; set; } = "active";
    public string? JoinDate { get; set; }
    public List<int> Projects { get; set; } = [];
    public decimal? Salary { get; set; }
    public string? Avatar { get; set; }
    public string? AvatarColor { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public bool IsAdmin { get; set; }
    public string UserType { get; set; } = "Employee";
}

public class CreateEmployeeRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Dept { get; set; }
    public string? JoinDate { get; set; }
    public decimal? Salary { get; set; }
    public string Status { get; set; } = "active";
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Avatar { get; set; }
    public string? AvatarColor { get; set; }
    public bool IsAdmin { get; set; }
    public string UserType { get; set; } = "Employee";
}

public class UpdateEmployeeRequest
{
    public string? Name { get; set; }
    [EmailAddress] public string? Email { get; set; }
    public string? Role { get; set; }
    public string? Dept { get; set; }
    public string? JoinDate { get; set; }
    public decimal? Salary { get; set; }
    public string? Status { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Avatar { get; set; }
    public string? AvatarColor { get; set; }
    public bool? IsAdmin { get; set; }
    public string? UserType { get; set; }
}

public class UpdateProfileRequest
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    [EmailAddress] public string? Email { get; set; }
}

public class AssignRolesRequest
{
    [Required] public List<string> Roles { get; set; } = [];
}

public class AssignProjectsRequest
{
    [Required] public List<int> ProjectIds { get; set; } = [];
}

public class SalaryHistoryDto
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class EmploymentHistoryDto
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Dept { get; set; }
    public string? Notes { get; set; }
}

public class AvatarUploadResponse
{
    public string AvatarUrl { get; set; } = string.Empty;
}

public class EmployeeListQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Dept { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
