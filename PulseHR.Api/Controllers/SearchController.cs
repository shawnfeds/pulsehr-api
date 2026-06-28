using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Data;
using PulseHR.Api.DTOs.Common;
using PulseHR.Api.DTOs.Search;

namespace PulseHR.Api.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController(PulseHRContext db) : ControllerBase
{
    // GET api/search?q=...
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SearchResultDto>>), 200)]
    public async Task<IActionResult> Search([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return BadRequest(ApiResponse<object>.Fail("Query must be at least 2 characters."));

        var term = q.Trim();

        var employees = await db.Employees
            .AsNoTracking()
            .Where(e => e.Name.Contains(term) || e.Email.Contains(term))
            .Take(10)
            .Select(e => new SearchResultDto
            {
                Type   = "employee",
                Id     = e.EmployeeId,
                Label  = e.Name,
                Sub    = e.Role,
                Avatar = e.AvatarText,
                Color  = e.AvatarColor
            })
            .ToListAsync();

        var projects = await db.Projects
            .AsNoTracking()
            .Where(p => p.Name.Contains(term))
            .Take(10)
            .Select(p => new SearchResultDto
            {
                Type  = "project",
                Id    = p.ProjectId,
                Label = p.Name,
                Sub   = p.Status
            })
            .ToListAsync();

        var results = employees.Concat(projects).ToList();
        return Ok(ApiResponse<PagedResult<SearchResultDto>>.Ok(new PagedResult<SearchResultDto>
        {
            Data  = results,
            Total = results.Count
        }));
    }
}
