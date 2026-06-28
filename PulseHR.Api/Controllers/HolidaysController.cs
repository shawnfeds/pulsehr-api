using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Data;
using PulseHR.Api.DTOs.Common;
using PulseHR.Api.DTOs.Holidays;
using PulseHR.Api.Models;

namespace PulseHR.Api.Controllers;

[ApiController]
[Route("api/holidays")]
[Authorize]
public class HolidaysController(PulseHRContext db) : ControllerBase
{
    // GET api/holidays
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<HolidayDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int? year)
    {
        var query = db.Holidays.AsNoTracking().AsQueryable();

        if (year.HasValue)
            query = query.Where(h => h.Date.Year == year.Value);

        var holidays = await query.OrderBy(h => h.Date).ToListAsync();
        return Ok(ApiResponse<PagedResult<HolidayDto>>.Ok(new PagedResult<HolidayDto>
        {
            Data  = holidays.Select(MapToDto),
            Total = holidays.Count
        }));
    }

    // POST api/holidays
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<HolidayDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateHolidayRequest req)
    {
        var holiday = new Holiday
        {
            Name        = req.Name,
            Date        = DateOnly.Parse(req.Date),
            Type        = req.Type,
            Description = req.Description
        };

        db.Holidays.Add(holiday);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), ApiResponse<HolidayDto>.Ok(MapToDto(holiday)));
    }

    // PUT api/holidays/:id
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<HolidayDto>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHolidayRequest req)
    {
        var holiday = await db.Holidays.FindAsync(id)
            ?? throw new KeyNotFoundException($"Holiday {id} not found.");

        if (req.Name        is not null) holiday.Name        = req.Name;
        if (req.Date        is not null) holiday.Date        = DateOnly.Parse(req.Date);
        if (req.Type        is not null) holiday.Type        = req.Type;
        if (req.Description is not null) holiday.Description = req.Description;

        await db.SaveChangesAsync();
        return Ok(ApiResponse<HolidayDto>.Ok(MapToDto(holiday)));
    }

    // DELETE api/holidays/:id
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Delete(int id)
    {
        var holiday = await db.Holidays.FindAsync(id)
            ?? throw new KeyNotFoundException($"Holiday {id} not found.");
        db.Holidays.Remove(holiday);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("Holiday deleted."));
    }

    private static HolidayDto MapToDto(Holiday h) => new()
    {
        Id          = h.HolidayId,
        Name        = h.Name,
        Date        = h.Date.ToString("yyyy-MM-dd"),
        Type        = h.Type,
        Description = h.Description
    };
}
