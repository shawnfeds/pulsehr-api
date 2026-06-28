using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Data;
using PulseHR.Api.DTOs.Common;
using PulseHR.Api.DTOs.Leaves;
using PulseHR.Api.Models;

namespace PulseHR.Api.Controllers;

[ApiController]
[Route("api/leaves")]
[Authorize]
public class LeavesController(PulseHRContext db) : ControllerBase
{
    // GET api/leaves  (admin only — all employees' leaves)
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LeaveDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] LeaveListQuery q)
    {
        var query = db.Leaves.AsNoTracking().Include(l => l.Employee).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Status)) query = query.Where(l => l.Status == q.Status);
        if (!string.IsNullOrWhiteSpace(q.Type))   query = query.Where(l => l.LeaveType == q.Type);

        var total  = await query.CountAsync();
        var leaves = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<LeaveDto>>.Ok(new PagedResult<LeaveDto>
        {
            Data  = leaves.Select(MapToDto),
            Total = total
        }));
    }

    // GET api/leaves/employee/:employeeId
    [HttpGet("employee/{employeeId:int}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LeaveDto>>), 200)]
    public async Task<IActionResult> GetByEmployee(int employeeId, [FromQuery] string? status)
    {
        var callerId = GetEmployeeId();
        var isAdmin  = User.HasClaim("isAdmin", "true");
        if (!isAdmin && callerId != employeeId) return Forbid();

        var query = db.Leaves.AsNoTracking().Include(l => l.Employee)
            .Where(l => l.EmployeeId == employeeId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(l => l.Status == status);

        var leaves = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
        return Ok(ApiResponse<PagedResult<LeaveDto>>.Ok(new PagedResult<LeaveDto>
        {
            Data  = leaves.Select(MapToDto),
            Total = leaves.Count
        }));
    }

    // GET api/leaves/employee/:employeeId/balance
    [HttpGet("employee/{employeeId:int}/balance")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, LeaveBalanceDto>>), 200)]
    public async Task<IActionResult> GetBalance(int employeeId)
    {
        var callerId = GetEmployeeId();
        var isAdmin  = User.HasClaim("isAdmin", "true");
        if (!isAdmin && callerId != employeeId) return Forbid();

        var balances = await db.LeaveBalances
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId)
            .ToListAsync();

        var result = balances.ToDictionary(
            b => b.LeaveType.ToLower(),
            b => new LeaveBalanceDto
            {
                Total   = b.Total,
                Used    = b.Used,
                Balance = b.Balance ?? (b.Total - b.Used)
            }
        );

        return Ok(ApiResponse<Dictionary<string, LeaveBalanceDto>>.Ok(result));
    }

    // GET api/leaves/:id
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<LeaveDto>), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        var leave = await db.Leaves.AsNoTracking().Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.LeaveId == id)
            ?? throw new KeyNotFoundException($"Leave {id} not found.");
        return Ok(ApiResponse<LeaveDto>.Ok(MapToDto(leave)));
    }

    // POST api/leaves
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LeaveDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequest req)
    {
        var leave = new Leave
        {
            EmployeeId = req.EmployeeId,
            LeaveType  = req.Type,
            StartDate  = DateOnly.Parse(req.StartDate),
            EndDate    = DateOnly.Parse(req.EndDate),
            Days       = req.Days,
            HalfDay    = req.HalfDay,
            Reason     = req.Reason,
            Status     = "pending",
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow
        };

        db.Leaves.Add(leave);
        await db.SaveChangesAsync();
        await db.Entry(leave).Reference(l => l.Employee).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = leave.LeaveId },
            ApiResponse<LeaveDto>.Ok(MapToDto(leave)));
    }

    // PATCH api/leaves/:id/status  (admin only)
    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<LeaveDto>), 200)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLeaveStatusRequest req)
    {
        if (req.Status is not ("approved" or "rejected"))
            return BadRequest(ApiResponse<object>.Fail("Status must be 'approved' or 'rejected'."));

        var leave = await db.Leaves.Include(l => l.Employee).FirstOrDefaultAsync(l => l.LeaveId == id)
            ?? throw new KeyNotFoundException($"Leave {id} not found.");

        var wasApproved = leave.Status == "approved";
        leave.Status    = req.Status;
        leave.Remarks   = req.Remarks;
        leave.UpdatedAt = DateTime.UtcNow;

        // Update leave balance
        var balance = await db.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == leave.EmployeeId && b.LeaveType == leave.LeaveType);

        if (balance is not null)
        {
            if (req.Status == "approved" && !wasApproved)
                balance.Used += leave.Days;
            else if (req.Status == "rejected" && wasApproved)
                balance.Used = Math.Max(0, balance.Used - leave.Days);
        }

        await db.SaveChangesAsync();
        return Ok(ApiResponse<LeaveDto>.Ok(MapToDto(leave)));
    }

    // DELETE api/leaves/:id
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Delete(int id)
    {
        var leave = await db.Leaves.FindAsync(id)
            ?? throw new KeyNotFoundException($"Leave {id} not found.");
        db.Leaves.Remove(leave);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("Leave deleted."));
    }

    private static LeaveDto MapToDto(Leave l) => new()
    {
        Id           = l.LeaveId,
        EmployeeId   = l.EmployeeId,
        EmployeeName = l.Employee?.Name,
        Type         = l.LeaveType,
        StartDate    = l.StartDate.ToString("yyyy-MM-dd"),
        EndDate      = l.EndDate.ToString("yyyy-MM-dd"),
        Days         = l.Days,
        Status       = l.Status,
        Reason       = l.Reason,
        HalfDay      = l.HalfDay,
        Remarks      = l.Remarks
    };

    private int? GetEmployeeId()
    {
        var claim = User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
