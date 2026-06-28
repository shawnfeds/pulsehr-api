using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

[Index("EmployeeId", Name = "IX_Notifications_EmployeeId")]
public partial class Notification
{
    [Key]
    public int NotificationId { get; set; }

    public int EmployeeId { get; set; }

    [StringLength(300)]
    public string Title { get; set; } = null!;

    [StringLength(1000)]
    public string? Body { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Notifications")]
    public virtual Employee Employee { get; set; } = null!;
}
