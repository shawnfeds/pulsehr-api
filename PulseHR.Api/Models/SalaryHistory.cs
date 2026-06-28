using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

[Table("SalaryHistory")]
public partial class SalaryHistory
{
    [Key]
    public int SalaryHistoryId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly Date { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("SalaryHistories")]
    public virtual Employee Employee { get; set; } = null!;
}
