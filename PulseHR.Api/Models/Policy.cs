using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

public partial class Policy
{
    [Key]
    public int PolicyId { get; set; }

    [StringLength(300)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string Category { get; set; } = null!;

    public int? UploadedByEmployeeId { get; set; }

    public DateTime UploadedOn { get; set; }

    [StringLength(50)]
    public string? FileSize { get; set; }

    [StringLength(100)]
    public string? ContentType { get; set; }

    [StringLength(1000)]
    public string FilePath { get; set; } = null!;

    [StringLength(1000)]
    public string? Notes { get; set; }

    [ForeignKey("UploadedByEmployeeId")]
    [InverseProperty("Policies")]
    public virtual Employee? UploadedByEmployee { get; set; }
}
