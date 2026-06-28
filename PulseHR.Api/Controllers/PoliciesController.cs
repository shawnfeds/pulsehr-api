using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Data;
using PulseHR.Api.DTOs.Common;
using PulseHR.Api.DTOs.Policies;
using PulseHR.Api.Models;
using PulseHR.Api.Services;

namespace PulseHR.Api.Controllers;

[ApiController]
[Route("api/policies")]
[Authorize]
public class PoliciesController(PulseHRContext db, IFileStorageService fileStorage) : ControllerBase
{
    // GET api/policies
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PolicyDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] PolicyListQuery q)
    {
        var query = db.Policies.AsNoTracking().Include(p => p.UploadedByEmployee).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Category))
            query = query.Where(p => p.Category == q.Category);

        var policies = await query.OrderByDescending(p => p.UploadedOn).ToListAsync();
        return Ok(ApiResponse<PagedResult<PolicyDto>>.Ok(new PagedResult<PolicyDto>
        {
            Data  = policies.Select(MapToDto),
            Total = policies.Count
        }));
    }

    // GET api/policies/:id
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PolicyDto>), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        var policy = await db.Policies.AsNoTracking()
            .Include(p => p.UploadedByEmployee)
            .FirstOrDefaultAsync(p => p.PolicyId == id)
            ?? throw new KeyNotFoundException($"Policy {id} not found.");
        return Ok(ApiResponse<PolicyDto>.Ok(MapToDto(policy)));
    }

    // POST api/policies/upload
    [HttpPost("upload")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("UploadLimit")]
    [ProducesResponseType(typeof(ApiResponse<PolicyDto>), 201)]
    public async Task<IActionResult> Upload([FromForm] string name, [FromForm] string category, IFormFile file)
    {
        if (file.Length == 0)         return BadRequest(ApiResponse<object>.Fail("No file provided."));
        if (file.Length > 20 * 1024 * 1024) return BadRequest(ApiResponse<object>.Fail("File too large (max 20 MB)."));

        var uploaderId = GetEmployeeId();
        var filePath   = await fileStorage.SaveFileAsync(file, "policies");
        var sizeKb     = $"{file.Length / 1024.0:F0} KB";
        var ext        = Path.GetExtension(file.FileName).TrimStart('.').ToLower();

        var policy = new Policy
        {
            Name                 = name,
            Category             = category,
            UploadedByEmployeeId = uploaderId,
            UploadedOn           = DateTime.UtcNow,
            FileSize             = sizeKb,
            ContentType          = file.ContentType,
            FilePath             = filePath
        };

        db.Policies.Add(policy);
        await db.SaveChangesAsync();

        if (uploaderId.HasValue)
            await db.Entry(policy).Reference(p => p.UploadedByEmployee).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = policy.PolicyId },
            ApiResponse<PolicyDto>.Ok(MapToDto(policy)));
    }

    // DELETE api/policies/:id
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Delete(int id)
    {
        var policy = await db.Policies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Policy {id} not found.");

        await fileStorage.DeleteFileAsync(policy.FilePath);
        db.Policies.Remove(policy);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("Policy deleted."));
    }

    // GET api/policies/:id/download
    [HttpGet("{id:int}/download")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Download(int id)
    {
        var policy = await db.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.PolicyId == id)
            ?? throw new KeyNotFoundException($"Policy {id} not found.");

        var stream = fileStorage.OpenRead(policy.FilePath);
        if (stream is null) return NotFound(ApiResponse<object>.Fail("File not found on server."));

        var contentType = fileStorage.GetContentType(policy.FilePath);
        var fileName    = Uri.EscapeDataString(policy.Name) + Path.GetExtension(policy.FilePath);
        return File(stream, contentType, fileName);
    }

    private static PolicyDto MapToDto(Policy p) => new()
    {
        Id         = p.PolicyId,
        Name       = p.Name,
        Category   = p.Category,
        UploadedBy = p.UploadedByEmployee?.Name,
        UploadedOn = p.UploadedOn.ToString("yyyy-MM-dd"),
        Size       = p.FileSize,
        Type       = Path.GetExtension(p.FilePath).TrimStart('.').ToLower(),
        Notes      = p.Notes
    };

    private int? GetEmployeeId()
    {
        var claim = User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
