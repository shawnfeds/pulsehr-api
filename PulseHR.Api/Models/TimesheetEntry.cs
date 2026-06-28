using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

[Index("EmployeeId", Name = "IX_Timesheet_EmployeeId")]
[Index("Month", Name = "IX_Timesheet_Month")]
public partial class TimesheetEntry
{
    [Key]
    public int TimesheetId { get; set; }

    public int EmployeeId { get; set; }

    public int ProjectId { get; set; }

    public DateOnly Date { get; set; }

    [StringLength(500)]
    public string Task { get; set; } = null!;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal Hours { get; set; }

    [StringLength(30)]
    public string Month { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("TimesheetEntries")]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey("ProjectId")]
    [InverseProperty("TimesheetEntries")]
    public virtual Project Project { get; set; } = null!;
}
