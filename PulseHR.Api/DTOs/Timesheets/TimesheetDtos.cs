using System.ComponentModel.DataAnnotations;

namespace PulseHR.Api.DTOs.Timesheets;

public class TimesheetDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Date { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string? Project { get; set; }
    public string Task { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public string Month { get; set; } = string.Empty;
}

public class CreateTimesheetRequest
{
    [Required] public int EmployeeId { get; set; }
    [Required] public string Date { get; set; } = string.Empty;
    [Required] public int ProjectId { get; set; }
    public string? Project { get; set; }
    [Required] public string Task { get; set; } = string.Empty;
    [Required, Range(0.25, 24)] public decimal Hours { get; set; }
    [Required] public string Month { get; set; } = string.Empty;
}

public class UpdateTimesheetRequest
{
    public string? Date { get; set; }
    public int? ProjectId { get; set; }
    public string? Project { get; set; }
    public string? Task { get; set; }
    [Range(0.25, 24)] public decimal? Hours { get; set; }
    public string? Month { get; set; }
}

public class TimesheetListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class EmployeeTimesheetQuery
{
    public string? Month { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// Used by CsvHelper for export
public class TimesheetCsvRecord
{
    public string Date { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public string Month { get; set; } = string.Empty;
}
