using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Data;
using PulseHR.Api.DTOs.Common;
using PulseHR.Api.DTOs.Projects;
using PulseHR.Api.Models;

namespace PulseHR.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController(PulseHRContext db) : ControllerBase
{
    // GET api/projects
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProjectDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] ProjectListQuery q)
    {
        var query = db.Projects.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Status))
            query = query.Where(p => p.Status == q.Status);
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(p => p.Name.Contains(q.Search));

        var total = await query.CountAsync();
        var projects = await query
            .Include(p => p.ManagerEmployee)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        var projectIds = projects.Select(p => p.ProjectId).ToList();
        var memberMap = await db.ProjectMembers
            .Where(pm => projectIds.Contains(pm.ProjectId))
            .GroupBy(pm => pm.ProjectId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(pm => pm.EmployeeId).ToList());

        var dtos = projects.Select(p => MapToDto(p, memberMap.GetValueOrDefault(p.ProjectId, [])));
        return Ok(ApiResponse<PagedResult<ProjectDto>>.Ok(new PagedResult<ProjectDto> { Data = dtos, Total = total }));
    }

    // GET api/projects/:id
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await db.Projects
            .AsNoTracking()
            .Include(p => p.ManagerEmployee)
            .FirstOrDefaultAsync(p => p.ProjectId == id)
            ?? throw new KeyNotFoundException($"Project {id} not found.");

        var members = await db.ProjectMembers.Where(pm => pm.ProjectId == id).Select(pm => pm.EmployeeId).ToListAsync();
        return Ok(ApiResponse<ProjectDto>.Ok(MapToDto(project, members)));
    }

    // GET api/projects/employee/:employeeId
    [HttpGet("employee/{employeeId:int}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProjectDto>>), 200)]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var projectIds = await db.ProjectMembers
            .Where(pm => pm.EmployeeId == employeeId)
            .Select(pm => pm.ProjectId)
            .ToListAsync();

        var projects = await db.Projects
            .AsNoTracking()
            .Include(p => p.ManagerEmployee)
            .Where(p => projectIds.Contains(p.ProjectId))
            .ToListAsync();

        var memberMap = await db.ProjectMembers
            .Where(pm => projectIds.Contains(pm.ProjectId))
            .GroupBy(pm => pm.ProjectId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(pm => pm.EmployeeId).ToList());

        var dtos = projects.Select(p => MapToDto(p, memberMap.GetValueOrDefault(p.ProjectId, [])));
        return Ok(ApiResponse<PagedResult<ProjectDto>>.Ok(new PagedResult<ProjectDto> { Data = dtos, Total = projects.Count }));
    }

    // POST api/projects
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest req)
    {
        // Resolve manager by name if ManagerId not provided
        int? managerId = req.ManagerId;
        if (managerId is null && !string.IsNullOrWhiteSpace(req.Manager))
        {
            managerId = await db.Employees
                .Where(e => e.Name == req.Manager)
                .Select(e => (int?)e.EmployeeId)
                .FirstOrDefaultAsync();
        }

        var project = new Project
        {
            Name               = req.Name,
            Description        = req.Description,
            ManagerEmployeeId  = managerId,
            Status             = req.Status,
            StartDate          = req.StartDate is not null ? DateOnly.Parse(req.StartDate) : null,
            EndDate            = req.EndDate   is not null ? DateOnly.Parse(req.EndDate)   : null,
            Progress           = req.Progress,
            CreatedAt          = DateTime.UtcNow,
            UpdatedAt          = DateTime.UtcNow
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();
        await db.Entry(project).Reference(p => p.ManagerEmployee).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = project.ProjectId },
            ApiResponse<ProjectDto>.Ok(MapToDto(project, [])));
    }

    // PUT api/projects/:id
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectRequest req)
    {
        var project = await db.Projects.Include(p => p.ManagerEmployee).FirstOrDefaultAsync(p => p.ProjectId == id)
            ?? throw new KeyNotFoundException($"Project {id} not found.");

        if (req.Name        is not null) project.Name        = req.Name;
        if (req.Description is not null) project.Description = req.Description;
        if (req.Status      is not null) project.Status      = req.Status;
        if (req.Progress    is not null) project.Progress    = req.Progress.Value;
        if (req.StartDate   is not null) project.StartDate   = DateOnly.Parse(req.StartDate);
        if (req.EndDate     is not null) project.EndDate     = DateOnly.Parse(req.EndDate);
        if (req.ManagerId   is not null) project.ManagerEmployeeId = req.ManagerId;
        project.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        var members = await db.ProjectMembers.Where(pm => pm.ProjectId == id).Select(pm => pm.EmployeeId).ToListAsync();
        return Ok(ApiResponse<ProjectDto>.Ok(MapToDto(project, members)));
    }

    // DELETE api/projects/:id
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await db.Projects.FindAsync(id)
            ?? throw new KeyNotFoundException($"Project {id} not found.");
        db.Projects.Remove(project);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("Project deleted."));
    }

    // POST api/projects/:id/members
    [HttpPost("{id:int}/members")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 200)]
    public async Task<IActionResult> AddMembers(int id, [FromBody] ProjectMembersRequest req)
    {
        var project = await db.Projects.Include(p => p.ManagerEmployee).FirstOrDefaultAsync(p => p.ProjectId == id)
            ?? throw new KeyNotFoundException($"Project {id} not found.");

        var existing = await db.ProjectMembers.Where(pm => pm.ProjectId == id).Select(pm => pm.EmployeeId).ToListAsync();
        var toAdd    = req.EmployeeIds.Except(existing).ToList();

        db.ProjectMembers.AddRange(toAdd.Select(eid => new ProjectMember
        {
            ProjectId  = id,
            EmployeeId = eid,
            AssignedOn = DateTime.UtcNow
        }));

        await db.SaveChangesAsync();
        var allMembers = await db.ProjectMembers.Where(pm => pm.ProjectId == id).Select(pm => pm.EmployeeId).ToListAsync();
        return Ok(ApiResponse<ProjectDto>.Ok(MapToDto(project, allMembers)));
    }

    // DELETE api/projects/:id/members
    [HttpDelete("{id:int}/members")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 200)]
    public async Task<IActionResult> RemoveMembers(int id, [FromBody] ProjectMembersRequest req)
    {
        var project = await db.Projects.Include(p => p.ManagerEmployee).FirstOrDefaultAsync(p => p.ProjectId == id)
            ?? throw new KeyNotFoundException($"Project {id} not found.");

        var toRemove = await db.ProjectMembers
            .Where(pm => pm.ProjectId == id && req.EmployeeIds.Contains(pm.EmployeeId))
            .ToListAsync();
        db.ProjectMembers.RemoveRange(toRemove);

        await db.SaveChangesAsync();
        var allMembers = await db.ProjectMembers.Where(pm => pm.ProjectId == id).Select(pm => pm.EmployeeId).ToListAsync();
        return Ok(ApiResponse<ProjectDto>.Ok(MapToDto(project, allMembers)));
    }

    private static ProjectDto MapToDto(Project p, List<int> members) => new()
    {
        Id          = p.ProjectId,
        Name        = p.Name,
        Manager     = p.ManagerEmployee?.Name,
        ManagerId   = p.ManagerEmployeeId,
        Status      = p.Status,
        StartDate   = p.StartDate?.ToString("yyyy-MM-dd"),
        EndDate     = p.EndDate?.ToString("yyyy-MM-dd"),
        Progress    = p.Progress,
        Members     = members,
        Description = p.Description
    };
}
