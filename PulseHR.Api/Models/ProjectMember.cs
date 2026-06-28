using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

[Index("ProjectId", "EmployeeId", Name = "UQ_ProjectMembers", IsUnique = true)]
public partial class ProjectMember
{
    [Key]
    public int ProjectMemberId { get; set; }

    public int ProjectId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime AssignedOn { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("ProjectMembers")]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey("ProjectId")]
    [InverseProperty("ProjectMembers")]
    public virtual Project Project { get; set; } = null!;
}
