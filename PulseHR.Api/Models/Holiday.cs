using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

[Index("Date", Name = "IX_Holidays_Date")]
public partial class Holiday
{
    [Key]
    public int HolidayId { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    public DateOnly Date { get; set; }

    [StringLength(50)]
    public string Type { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }
}
