using System.ComponentModel.DataAnnotations;

namespace PulseHR.Api.DTOs.Projects;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Manager { get; set; }
    public int? ManagerId { get; set; }
    public string Status { get; set; } = "active";
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public int Progress { get; set; }
    public List<int> Members { get; set; } = [];
    public string? Description { get; set; }
}

public class CreateProjectRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Manager { get; set; }
    public int? ManagerId { get; set; }
    public string Status { get; set; } = "active";
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public int Progress { get; set; } = 0;
}

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Manager { get; set; }
    public int? ManagerId { get; set; }
    public string? Status { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public int? Progress { get; set; }
}

public class ProjectMembersRequest
{
    [Required] public List<int> EmployeeIds { get; set; } = [];
}

public class ProjectListQuery
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
