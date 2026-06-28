using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

[Index("EmployeeId", "RoleId", Name = "UQ_EmployeeRoles", IsUnique = true)]
public partial class EmployeeRole
{
    [Key]
    public int EmployeeRoleId { get; set; }

    public int EmployeeId { get; set; }

    public int RoleId { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("EmployeeRoles")]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("EmployeeRoles")]
    public virtual Role Role { get; set; } = null!;
}
