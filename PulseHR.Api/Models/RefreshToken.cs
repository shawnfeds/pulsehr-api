using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PulseHR.Api.Models;

[Index("EmployeeId", Name = "IX_RefreshTokens_EmployeeId")]
[Index("Token", Name = "IX_RefreshTokens_Token")]
[Index("Token", Name = "UQ_RefreshTokens_Token", IsUnique = true)]
public partial class RefreshToken
{
    [Key]
    public int RefreshTokenId { get; set; }

    public int EmployeeId { get; set; }

    [StringLength(512)]
    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    [StringLength(512)]
    public string? ReplacedByToken { get; set; }

    [StringLength(50)]
    public string? CreatedByIp { get; set; }

    [StringLength(50)]
    public string? RevokedByIp { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("RefreshTokens")]
    public virtual Employee Employee { get; set; } = null!;
}
