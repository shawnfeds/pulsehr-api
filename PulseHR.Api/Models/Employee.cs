using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

[Index("Dept", Name = "IX_Employees_Dept")]
[Index("Email", Name = "IX_Employees_Email")]
[Index("Status", Name = "IX_Employees_Status")]
[Index("Email", Name = "UQ_Employees_Email", IsUnique = true)]
public partial class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(256)]
    public string Email { get; set; } = null!;

    [StringLength(512)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(512)]
    public string PasswordSalt { get; set; } = null!;

    public bool IsAdmin { get; set; }

    [StringLength(50)]
    public string UserType { get; set; } = null!;

    [StringLength(200)]
    public string? Role { get; set; }

    [StringLength(200)]
    public string? Dept { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    public DateOnly? JoinDate { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(10)]
    public string? AvatarText { get; set; }

    [StringLength(20)]
    public string? AvatarColor { get; set; }

    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Salary { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Employee")]
    public virtual ICollection<EmployeeRole> EmployeeRoles { get; set; } = new List<EmployeeRole>();

    [InverseProperty("Employee")]
    public virtual ICollection<EmploymentHistory> EmploymentHistories { get; set; } = new List<EmploymentHistory>();

    [InverseProperty("Employee")]
    public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();

    [InverseProperty("Employee")]
    public virtual ICollection<Leave> Leaves { get; set; } = new List<Leave>();

    [InverseProperty("Employee")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("UploadedByEmployee")]
    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();

    [InverseProperty("Employee")]
    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    [InverseProperty("ManagerEmployee")]
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    [InverseProperty("Employee")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [InverseProperty("Employee")]
    public virtual ICollection<SalaryHistory> SalaryHistories { get; set; } = new List<SalaryHistory>();

    [InverseProperty("Employee")]
    public virtual ICollection<TimesheetEntry> TimesheetEntries { get; set; } = new List<TimesheetEntry>();
}
