using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Data;
using PulseHR.Api.DTOs.Common;
using PulseHR.Api.DTOs.Events;
using PulseHR.Api.Models;

namespace PulseHR.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController(PulseHRContext db) : ControllerBase
{
    // GET api/events
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EventDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var events = await db.Events.AsNoTracking()
            .OrderBy(e => e.Date)
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<EventDto>>.Ok(new PagedResult<EventDto>
        {
            Data  = events.Select(MapToDto),
            Total = events.Count
        }));
    }

    // POST api/events
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<EventDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest req)
    {
        var ev = new Event
        {
            Name        = req.Name,
            Description = req.Description,
            Date        = DateTime.Parse(req.Date, null, System.Globalization.DateTimeStyles.RoundtripKind),
            Type        = req.Type,
            Location    = req.Location
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), ApiResponse<EventDto>.Ok(MapToDto(ev)));
    }

    // PUT api/events/:id
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<EventDto>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest req)
    {
        var ev = await db.Events.FindAsync(id)
            ?? throw new KeyNotFoundException($"Event {id} not found.");

        if (req.Name        is not null) ev.Name        = req.Name;
        if (req.Description is not null) ev.Description = req.Description;
        if (req.Date        is not null) ev.Date        = DateTime.Parse(req.Date, null, System.Globalization.DateTimeStyles.RoundtripKind);
        if (req.Type        is not null) ev.Type        = req.Type;
        if (req.Location    is not null) ev.Location    = req.Location;

        await db.SaveChangesAsync();
        return Ok(ApiResponse<EventDto>.Ok(MapToDto(ev)));
    }

    // DELETE api/events/:id
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await db.Events.FindAsync(id)
            ?? throw new KeyNotFoundException($"Event {id} not found.");
        db.Events.Remove(ev);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("Event deleted."));
    }

    private static EventDto MapToDto(Event e) => new()
    {
        Id          = e.EventId,
        Name        = e.Name,
        Description = e.Description,
        Date        = e.Date.ToString("yyyy-MM-dd"),
        Type        = e.Type,
        Location    = e.Location
    };
}
