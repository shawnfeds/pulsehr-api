namespace PulseHR.Api.DTOs.Policies;

public class PolicyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? UploadedBy { get; set; }
    public string? UploadedOn { get; set; }
    public string? Size { get; set; }
    public string? Type { get; set; }
    public string? Notes { get; set; }
}

public class PolicyListQuery
{
    public string? Category { get; set; }
}
