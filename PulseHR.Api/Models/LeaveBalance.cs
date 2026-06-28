using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

[Index("EmployeeId", "LeaveType", Name = "UQ_LeaveBalances", IsUnique = true)]
public partial class LeaveBalance
{
    [Key]
    public int LeaveBalanceId { get; set; }

    public int EmployeeId { get; set; }

    [StringLength(50)]
    public string LeaveType { get; set; } = null!;

    [Column(TypeName = "decimal(5, 1)")]
    public decimal Total { get; set; }

    [Column(TypeName = "decimal(5, 1)")]
    public decimal Used { get; set; }

    [Column(TypeName = "decimal(6, 1)")]
    public decimal? Balance { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("LeaveBalances")]
    public virtual Employee Employee { get; set; } = null!;
}
