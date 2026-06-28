using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Data;
using PulseHR.Api.DTOs.Common;
using PulseHR.Api.DTOs.Dashboard;

namespace PulseHR.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(PulseHRContext db) : ControllerBase
{
    // GET api/dashboard/admin
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardDto>), 200)]
    public async Task<IActionResult> AdminDashboard()
    {
        var totalEmployees  = await db.Employees.CountAsync(e => e.Status == "active");
        var totalProjects   = await db.Projects.CountAsync();
        var activeProjects  = await db.Projects.CountAsync(p => p.Status == "active");
        var pendingLeaves   = await db.Leaves.CountAsync(l => l.Status == "pending");
        var totalDepts      = await db.Employees
            .Where(e => e.Dept != null)
            .Select(e => e.Dept!)
            .Distinct()
            .CountAsync();

        return Ok(ApiResponse<AdminDashboardDto>.Ok(new AdminDashboardDto
        {
            TotalEmployees   = totalEmployees,
            TotalProjects    = totalProjects,
            ActiveProjects   = activeProjects,
            PendingLeaves    = pendingLeaves,
            TotalDepartments = totalDepts
        }));
    }

    // GET api/dashboard/employee/:employeeId
    [HttpGet("employee/{employeeId:int}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDashboardDto>), 200)]
    public async Task<IActionResult> EmployeeDashboard(int employeeId)
    {
        var callerId = GetEmployeeId();
        var isAdmin  = User.HasClaim("isAdmin", "true");
        if (!isAdmin && callerId != employeeId) return Forbid();

        var now        = DateTime.UtcNow;
        var monthLabel = now.ToString("MMMM yyyy");  // e.g. "June 2026"

        var hoursThisMonth = await db.TimesheetEntries
            .Where(t => t.EmployeeId == employeeId && t.Month == monthLabel)
            .SumAsync(t => (decimal?)t.Hours) ?? 0;

        var leavesTaken = await db.Leaves
            .Where(l => l.EmployeeId == employeeId
                     && l.Status == "approved"
                     && l.StartDate.Year == now.Year)
            .SumAsync(l => (decimal?)l.Days) ?? 0;

        var activeProjects = await db.ProjectMembers
            .Where(pm => pm.EmployeeId == employeeId)
            .Join(db.Projects, pm => pm.ProjectId, p => p.ProjectId, (pm, p) => p)
            .CountAsync(p => p.Status == "active");

        // Simple attendance rate: worked days / working days this month (rough estimate)
        var workingDaysThisMonth = GetWorkingDaysInMonth(now.Year, now.Month);
        var daysLogged = await db.TimesheetEntries
            .Where(t => t.EmployeeId == employeeId && t.Month == monthLabel)
            .Select(t => t.Date)
            .Distinct()
            .CountAsync();

        var attendanceRate = workingDaysThisMonth > 0
            ? (int)Math.Min(100, Math.Round(daysLogged * 100.0 / workingDaysThisMonth))
            : 0;

        return Ok(ApiResponse<EmployeeDashboardDto>.Ok(new EmployeeDashboardDto
        {
            HoursThisMonth = hoursThisMonth,
            LeavesTaken    = leavesTaken,
            ActiveProjects = activeProjects,
            AttendanceRate = attendanceRate
        }));
    }

    private static int GetWorkingDaysInMonth(int year, int month)
    {
        var days = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Select(d => new DateTime(year, month, d))
            .Count(d => d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday);
        return days;
    }

    private int? GetEmployeeId()
    {
        var claim = User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
