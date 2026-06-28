using System.ComponentModel.DataAnnotations;

namespace PulseHR.Api.DTOs.Holidays;

public class HolidayDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateHolidayRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string Date { get; set; } = string.Empty;
    [Required] public string Type { get; set; } = "National";
    public string? Description { get; set; }
}

public class UpdateHolidayRequest
{
    public string? Name { get; set; }
    public string? Date { get; set; }
    public string? Type { get; set; }
    public string? Description { get; set; }
}
