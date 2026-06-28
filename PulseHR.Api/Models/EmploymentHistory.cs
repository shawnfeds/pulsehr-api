using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

[Table("EmploymentHistory")]
public partial class EmploymentHistory
{
    [Key]
    public int EmploymentHistoryId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly Date { get; set; }

    [StringLength(200)]
    public string Event { get; set; } = null!;

    [StringLength(200)]
    public string? Role { get; set; }

    [StringLength(200)]
    public string? Dept { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("EmploymentHistories")]
    public virtual Employee Employee { get; set; } = null!;
}
