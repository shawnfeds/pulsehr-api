using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Data;
using PulseHR.Api.DTOs.Common;
using PulseHR.Api.DTOs.Notifications;

namespace PulseHR.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(PulseHRContext db) : ControllerBase
{
    // GET api/notifications  (scoped to authenticated user)
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NotificationDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var employeeId = GetEmployeeId();
        if (employeeId is null) return Unauthorized();

        var notifications = await db.Notifications
            .AsNoTracking()
            .Where(n => n.EmployeeId == employeeId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        var dtos = notifications.Select(n => new NotificationDto
        {
            Id   = n.NotificationId,
            Text = n.Title + (n.Body is not null ? $" — {n.Body}" : string.Empty),
            Time = FormatTimeAgo(n.CreatedAt),
            Read = n.IsRead
        });

        return Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(new PagedResult<NotificationDto>
        {
            Data  = dtos,
            Total = notifications.Count
        }));
    }

    // PATCH api/notifications/:id/read
    [HttpPatch("{id:int}/read")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> MarkRead(int id)
    {
        var employeeId = GetEmployeeId();
        if (employeeId is null) return Unauthorized();

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == id && n.EmployeeId == employeeId.Value)
            ?? throw new KeyNotFoundException($"Notification {id} not found.");

        notification.IsRead = true;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("Notification marked as read."));
    }

    // POST api/notifications/read-all
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> MarkAllRead()
    {
        var employeeId = GetEmployeeId();
        if (employeeId is null) return Unauthorized();

        await db.Notifications
            .Where(n => n.EmployeeId == employeeId.Value && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return Ok(ApiResponse<object>.SuccessMessage("All notifications marked as read."));
    }

    private static string FormatTimeAgo(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;
        if (diff.TotalMinutes < 1)  return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
        if (diff.TotalHours   < 24) return $"{(int)diff.TotalHours} hr ago";
        return $"{(int)diff.TotalDays} day{((int)diff.TotalDays > 1 ? "s" : "")} ago";
    }

    private int? GetEmployeeId()
    {
        var claim = User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
