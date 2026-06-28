using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Data;
using PulseHR.Api.DTOs.Common;
using PulseHR.Api.DTOs.Employees;
using PulseHR.Api.Models;
using PulseHR.Api.Services;

namespace PulseHR.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController(PulseHRContext db, IFileStorageService fileStorage) : ControllerBase
{
    // GET api/employees
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EmployeeDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] EmployeeListQuery q)
    {
        var query = db.Employees.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(e => e.Name.Contains(q.Search) || e.Email.Contains(q.Search));
        if (!string.IsNullOrWhiteSpace(q.Status))
            query = query.Where(e => e.Status == q.Status);
        if (!string.IsNullOrWhiteSpace(q.Dept))
            query = query.Where(e => e.Dept == q.Dept);

        var total = await query.CountAsync();
        var employees = await query
            .OrderBy(e => e.Name)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        var employeeIds = employees.Select(e => e.EmployeeId).ToList();
        var projectMemberships = await db.ProjectMembers
            .Where(pm => employeeIds.Contains(pm.EmployeeId))
            .GroupBy(pm => pm.EmployeeId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(pm => pm.ProjectId).ToList());

        var dtos = employees.Select(e => MapToDto(e, projectMemberships.GetValueOrDefault(e.EmployeeId, [])));

        return Ok(ApiResponse<PagedResult<EmployeeDto>>.Ok(new PagedResult<EmployeeDto>
        {
            Data = dtos,
            Total = total
        }));
    }

    // GET api/employees/:id
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == id)
            ?? throw new KeyNotFoundException($"Employee {id} not found.");
        var projects = await db.ProjectMembers.Where(pm => pm.EmployeeId == id).Select(pm => pm.ProjectId).ToListAsync();
        return Ok(ApiResponse<EmployeeDto>.Ok(MapToDto(employee, projects)));
    }

    // GET api/employees/:id/profile
    [HttpGet("{id:int}/profile")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
    public async Task<IActionResult> GetProfile(int id) => await GetById(id);

    // POST api/employees
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest req)
    {
        if (await db.Employees.AnyAsync(e => e.Email == req.Email))
            return BadRequest(ApiResponse<object>.Fail("Email already in use."));

        var employee = new Employee
        {
            Name         = req.Name,
            Email        = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            PasswordSalt = string.Empty,  // BCrypt stores salt in hash
            Role         = req.Role,
            Dept         = req.Dept,
            JoinDate     = req.JoinDate is not null ? DateOnly.Parse(req.JoinDate) : null,
            Salary       = req.Salary,
            Status       = req.Status,
            Phone        = req.Phone,
            Location     = req.Location,
            AvatarText   = req.Avatar,
            AvatarColor  = req.AvatarColor,
            IsAdmin      = req.IsAdmin,
            UserType     = req.UserType,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        // Seed default leave balances
        db.LeaveBalances.AddRange(
            new LeaveBalance { EmployeeId = employee.EmployeeId, LeaveType = "Sick",   Total = 12, Used = 0 },
            new LeaveBalance { EmployeeId = employee.EmployeeId, LeaveType = "Casual", Total = 12, Used = 0 }
        );
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = employee.EmployeeId },
            ApiResponse<EmployeeDto>.Ok(MapToDto(employee, [])));
    }

    // PUT api/employees/:id
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest req)
    {
        var employee = await db.Employees.FindAsync(id)
            ?? throw new KeyNotFoundException($"Employee {id} not found.");

        if (req.Name     is not null) employee.Name     = req.Name;
        if (req.Email    is not null) employee.Email    = req.Email;
        if (req.Role     is not null) employee.Role     = req.Role;
        if (req.Dept     is not null) employee.Dept     = req.Dept;
        if (req.Status   is not null) employee.Status   = req.Status;
        if (req.Phone    is not null) employee.Phone    = req.Phone;
        if (req.Location is not null) employee.Location = req.Location;
        if (req.Avatar   is not null) employee.AvatarText  = req.Avatar;
        if (req.AvatarColor is not null) employee.AvatarColor = req.AvatarColor;
        if (req.Salary   is not null) employee.Salary   = req.Salary;
        if (req.IsAdmin  is not null) employee.IsAdmin  = req.IsAdmin.Value;
        if (req.UserType is not null) employee.UserType = req.UserType;
        if (req.JoinDate is not null) employee.JoinDate = DateOnly.Parse(req.JoinDate);
        employee.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        var projects = await db.ProjectMembers.Where(pm => pm.EmployeeId == id).Select(pm => pm.ProjectId).ToListAsync();
        return Ok(ApiResponse<EmployeeDto>.Ok(MapToDto(employee, projects)));
    }

    // PUT api/employees/:id/profile
    [HttpPut("{id:int}/profile")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequest req)
    {
        var callerId = GetEmployeeId();
        var isAdmin  = User.HasClaim("isAdmin", "true");
        if (!isAdmin && callerId != id) return Forbid();

        var employee = await db.Employees.FindAsync(id)
            ?? throw new KeyNotFoundException($"Employee {id} not found.");

        if (req.Name     is not null) employee.Name     = req.Name;
        if (req.Email    is not null) employee.Email    = req.Email;
        if (req.Phone    is not null) employee.Phone    = req.Phone;
        if (req.Location is not null) employee.Location = req.Location;
        employee.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        var projects = await db.ProjectMembers.Where(pm => pm.EmployeeId == id).Select(pm => pm.ProjectId).ToListAsync();
        return Ok(ApiResponse<EmployeeDto>.Ok(MapToDto(employee, projects)));
    }

    // DELETE api/employees/:id
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await db.Employees.FindAsync(id)
            ?? throw new KeyNotFoundException($"Employee {id} not found.");
        db.Employees.Remove(employee);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("Employee deleted."));
    }

    // POST api/employees/:id/roles
    [HttpPost("{id:int}/roles")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
    public async Task<IActionResult> AssignRoles(int id, [FromBody] AssignRolesRequest req)
    {
        var employee = await db.Employees.FindAsync(id)
            ?? throw new KeyNotFoundException($"Employee {id} not found.");

        var roleIds = await db.Roles
            .Where(r => req.Roles.Contains(r.Name))
            .Select(r => r.RoleId)
            .ToListAsync();

        var existing = await db.EmployeeRoles.Where(er => er.EmployeeId == id).ToListAsync();
        db.EmployeeRoles.RemoveRange(existing);

        db.EmployeeRoles.AddRange(roleIds.Select(rid => new EmployeeRole
        {
            EmployeeId = id,
            RoleId = rid
        }));

        await db.SaveChangesAsync();
        var projects = await db.ProjectMembers.Where(pm => pm.EmployeeId == id).Select(pm => pm.ProjectId).ToListAsync();
        return Ok(ApiResponse<EmployeeDto>.Ok(MapToDto(employee, projects)));
    }

    // POST api/employees/:id/projects
    [HttpPost("{id:int}/projects")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
    public async Task<IActionResult> AssignProjects(int id, [FromBody] AssignProjectsRequest req)
    {
        var employee = await db.Employees.FindAsync(id)
            ?? throw new KeyNotFoundException($"Employee {id} not found.");

        var existing = await db.ProjectMembers.Where(pm => pm.EmployeeId == id).ToListAsync();
        db.ProjectMembers.RemoveRange(existing);

        db.ProjectMembers.AddRange(req.ProjectIds.Select(pid => new ProjectMember
        {
            EmployeeId = id,
            ProjectId  = pid,
            AssignedOn = DateTime.UtcNow
        }));

        await db.SaveChangesAsync();
        return Ok(ApiResponse<EmployeeDto>.Ok(MapToDto(employee, req.ProjectIds)));
    }

    // GET api/employees/:id/salary-history
    [HttpGet("{id:int}/salary-history")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SalaryHistoryDto>>), 200)]
    public async Task<IActionResult> GetSalaryHistory(int id)
    {
        var records = await db.SalaryHistories
            .AsNoTracking()
            .Where(s => s.EmployeeId == id)
            .OrderByDescending(s => s.Date)
            .Select(s => new SalaryHistoryDto
            {
                Id     = s.SalaryHistoryId,
                Date   = s.Date.ToString("yyyy-MM-dd"),
                Amount = s.Amount,
                Note   = s.Note
            })
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<SalaryHistoryDto>>.Ok(new PagedResult<SalaryHistoryDto>
        {
            Data  = records,
            Total = records.Count
        }));
    }

    // GET api/employees/:id/employment-history
    [HttpGet("{id:int}/employment-history")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EmploymentHistoryDto>>), 200)]
    public async Task<IActionResult> GetEmploymentHistory(int id)
    {
        var callerId = GetEmployeeId();
        var isAdmin  = User.HasClaim("isAdmin", "true");
        if (!isAdmin && callerId != id) return Forbid();

        var records = await db.EmploymentHistories
            .AsNoTracking()
            .Where(h => h.EmployeeId == id)
            .OrderByDescending(h => h.Date)
            .Select(h => new EmploymentHistoryDto
            {
                Id    = h.EmploymentHistoryId,
                Date  = h.Date.ToString("yyyy-MM-dd"),
                Event = h.Event,
                Role  = h.Role,
                Dept  = h.Dept,
                Notes = h.Notes
            })
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<EmploymentHistoryDto>>.Ok(new PagedResult<EmploymentHistoryDto>
        {
            Data  = records,
            Total = records.Count
        }));
    }

    // POST api/employees/:id/avatar
    [HttpPost("{id:int}/avatar")]
    [ProducesResponseType(typeof(ApiResponse<AvatarUploadResponse>), 200)]
    public async Task<IActionResult> UploadAvatar(int id, IFormFile file)
    {
        var callerId = GetEmployeeId();
        var isAdmin  = User.HasClaim("isAdmin", "true");
        if (!isAdmin && callerId != id) return Forbid();

        var employee = await db.Employees.FindAsync(id)
            ?? throw new KeyNotFoundException($"Employee {id} not found.");

        if (file.Length == 0) return BadRequest(ApiResponse<object>.Fail("No file provided."));
        if (file.Length > 5 * 1024 * 1024) return BadRequest(ApiResponse<object>.Fail("File too large (max 5 MB)."));

        var url = await fileStorage.SaveFileAsync(file, "avatars");
        employee.AvatarUrl = url;
        employee.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ApiResponse<AvatarUploadResponse>.Ok(new AvatarUploadResponse { AvatarUrl = url }));
    }

    private static EmployeeDto MapToDto(Employee e, List<int> projects) => new()
    {
        Id          = e.EmployeeId,
        Name        = e.Name,
        Email       = e.Email,
        Role        = e.Role,
        Dept        = e.Dept,
        Status      = e.Status,
        JoinDate    = e.JoinDate?.ToString("yyyy-MM-dd"),
        Projects    = projects,
        Salary      = e.Salary,
        Avatar      = e.AvatarText,
        AvatarColor = e.AvatarColor,
        AvatarUrl   = e.AvatarUrl,
        Phone       = e.Phone,
        Location    = e.Location,
        IsAdmin     = e.IsAdmin,
        UserType    = e.UserType
    };

    private int? GetEmployeeId()
    {
        var claim = User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
