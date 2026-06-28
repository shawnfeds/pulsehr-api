using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

public partial class Event
{
    [Key]
    public int EventId { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime Date { get; set; }

    [StringLength(50)]
    public string Type { get; set; } = null!;

    [StringLength(300)]
    public string? Location { get; set; }
}
