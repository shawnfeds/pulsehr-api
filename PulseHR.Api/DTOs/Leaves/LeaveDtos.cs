using System.ComponentModel.DataAnnotations;

namespace PulseHR.Api.DTOs.Leaves;

public class LeaveDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string Type { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public decimal Days { get; set; }
    public string Status { get; set; } = "pending";
    public string? Reason { get; set; }
    public bool HalfDay { get; set; }
    public string? Remarks { get; set; }
}

public class CreateLeaveRequest
{
    [Required] public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    [Required] public string Type { get; set; } = string.Empty;
    [Required] public string StartDate { get; set; } = string.Empty;
    [Required] public string EndDate { get; set; } = string.Empty;
    [Required, Range(0.5, 365)] public decimal Days { get; set; }
    public string? Reason { get; set; }
    public bool HalfDay { get; set; }
}

public class UpdateLeaveStatusRequest
{
    [Required] public string Status { get; set; } = string.Empty; // approved | rejected
    public string? Remarks { get; set; }
}

public class LeaveBalanceDto
{
    public decimal Total { get; set; }
    public decimal Used { get; set; }
    public decimal Balance { get; set; }
}

public class LeaveListQuery
{
    public string? Status { get; set; }
    public string? Type { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
