using System.ComponentModel.DataAnnotations;

namespace PulseHR.Api.DTOs.Events;

public class EventDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public class CreateEventRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public string Date { get; set; } = string.Empty;
    [Required] public string Type { get; set; } = "Event";
    public string? Location { get; set; }
}

public class UpdateEventRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Date { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
}
