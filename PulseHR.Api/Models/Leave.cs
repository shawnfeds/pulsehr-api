using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

[Index("EmployeeId", Name = "IX_Leaves_EmployeeId")]
[Index("Status", Name = "IX_Leaves_Status")]
public partial class Leave
{
    [Key]
    public int LeaveId { get; set; }

    public int EmployeeId { get; set; }

    [StringLength(50)]
    public string LeaveType { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [Column(TypeName = "decimal(5, 1)")]
    public decimal Days { get; set; }

    public bool HalfDay { get; set; }

    [StringLength(1000)]
    public string? Reason { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [StringLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Leaves")]
    public virtual Employee Employee { get; set; } = null!;
}
