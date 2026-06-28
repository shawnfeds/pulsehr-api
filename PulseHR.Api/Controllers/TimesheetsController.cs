using System.Globalization;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Data;
using PulseHR.Api.DTOs.Common;
using PulseHR.Api.DTOs.Timesheets;
using PulseHR.Api.Models;

namespace PulseHR.Api.Controllers;

[ApiController]
[Route("api/timesheets")]
[Authorize]
public class TimesheetsController(PulseHRContext db) : ControllerBase
{
    // GET api/timesheets
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TimesheetDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] TimesheetListQuery q)
    {
        var total = await db.TimesheetEntries.CountAsync();
        var entries = await db.TimesheetEntries
            .AsNoTracking()
            .Include(t => t.Project)
            .OrderByDescending(t => t.Date)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<TimesheetDto>>.Ok(new PagedResult<TimesheetDto>
        {
            Data  = entries.Select(MapToDto),
            Total = total
        }));
    }

    // GET api/timesheets/employee/:employeeId
    [HttpGet("employee/{employeeId:int}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TimesheetDto>>), 200)]
    public async Task<IActionResult> GetByEmployee(int employeeId, [FromQuery] EmployeeTimesheetQuery q)
    {
        var callerId = GetEmployeeId();
        var isAdmin  = User.HasClaim("isAdmin", "true");
        if (!isAdmin && callerId != employeeId) return Forbid();

        var query = db.TimesheetEntries.AsNoTracking()
            .Include(t => t.Project)
            .Where(t => t.EmployeeId == employeeId);

        if (!string.IsNullOrWhiteSpace(q.Month))
            query = query.Where(t => t.Month == q.Month);

        var total   = await query.CountAsync();
        var entries = await query
            .OrderByDescending(t => t.Date)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<TimesheetDto>>.Ok(new PagedResult<TimesheetDto>
        {
            Data  = entries.Select(MapToDto),
            Total = total
        }));
    }

    // GET api/timesheets/employee/:employeeId/export
    [HttpGet("employee/{employeeId:int}/export")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Export(int employeeId, [FromQuery] string? month)
    {
        var callerId = GetEmployeeId();
        var isAdmin  = User.HasClaim("isAdmin", "true");
        if (!isAdmin && callerId != employeeId) return Forbid();

        var query = db.TimesheetEntries.AsNoTracking()
            .Include(t => t.Project)
            .Where(t => t.EmployeeId == employeeId);

        if (!string.IsNullOrWhiteSpace(month))
            query = query.Where(t => t.Month == month);

        var entries = await query.OrderBy(t => t.Date).ToListAsync();
        var records = entries.Select(t => new TimesheetCsvRecord
        {
            Date    = t.Date.ToString("yyyy-MM-dd"),
            Project = t.Project?.Name ?? string.Empty,
            Task    = t.Task,
            Hours   = t.Hours,
            Month   = t.Month
        });

        var stream = new MemoryStream();
        await using var writer = new StreamWriter(stream, leaveOpen: true);
        await using var csv    = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(records);
        await writer.FlushAsync();
        stream.Seek(0, SeekOrigin.Begin);

        var fileName = string.IsNullOrWhiteSpace(month)
            ? $"timesheet_employee_{employeeId}.csv"
            : $"timesheet_employee_{employeeId}_{month.Replace(" ", "_")}.csv";

        return File(stream, "text/csv", fileName);
    }

    // GET api/timesheets/:id
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TimesheetDto>), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        var entry = await db.TimesheetEntries.AsNoTracking()
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.TimesheetId == id)
            ?? throw new KeyNotFoundException($"Timesheet entry {id} not found.");
        return Ok(ApiResponse<TimesheetDto>.Ok(MapToDto(entry)));
    }

    // POST api/timesheets
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TimesheetDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateTimesheetRequest req)
    {
        var entry = new TimesheetEntry
        {
            EmployeeId = req.EmployeeId,
            ProjectId  = req.ProjectId,
            Date       = DateOnly.Parse(req.Date),
            Task       = req.Task,
            Hours      = req.Hours,
            Month      = req.Month,
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow
        };

        db.TimesheetEntries.Add(entry);
        await db.SaveChangesAsync();
        await db.Entry(entry).Reference(t => t.Project).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = entry.TimesheetId },
            ApiResponse<TimesheetDto>.Ok(MapToDto(entry)));
    }

    // PUT api/timesheets/:id
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TimesheetDto>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTimesheetRequest req)
    {
        var entry = await db.TimesheetEntries.Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.TimesheetId == id)
            ?? throw new KeyNotFoundException($"Timesheet entry {id} not found.");

        var callerId = GetEmployeeId();
        var isAdmin  = User.HasClaim("isAdmin", "true");
        if (!isAdmin && callerId != entry.EmployeeId) return Forbid();

        if (req.Date      is not null) entry.Date      = DateOnly.Parse(req.Date);
        if (req.ProjectId is not null) entry.ProjectId = req.ProjectId.Value;
        if (req.Task      is not null) entry.Task      = req.Task;
        if (req.Hours     is not null) entry.Hours     = req.Hours.Value;
        if (req.Month     is not null) entry.Month     = req.Month;
        entry.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ApiResponse<TimesheetDto>.Ok(MapToDto(entry)));
    }

    // DELETE api/timesheets/:id
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await db.TimesheetEntries.FindAsync(id)
            ?? throw new KeyNotFoundException($"Timesheet entry {id} not found.");

        var callerId = GetEmployeeId();
        var isAdmin  = User.HasClaim("isAdmin", "true");
        if (!isAdmin && callerId != entry.EmployeeId) return Forbid();

        db.TimesheetEntries.Remove(entry);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("Timesheet entry deleted."));
    }

    private static TimesheetDto MapToDto(TimesheetEntry t) => new()
    {
        Id         = t.TimesheetId,
        EmployeeId = t.EmployeeId,
        Date       = t.Date.ToString("yyyy-MM-dd"),
        ProjectId  = t.ProjectId,
        Project    = t.Project?.Name,
        Task       = t.Task,
        Hours      = t.Hours,
        Month      = t.Month
    };

    private int? GetEmployeeId()
    {
        var claim = User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
