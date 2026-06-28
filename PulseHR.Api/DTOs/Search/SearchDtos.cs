namespace PulseHR.Api.DTOs.Search;

public class SearchResultDto
{
    public string Type { get; set; } = string.Empty;  // "employee" | "project"
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Sub { get; set; }
    public string? Avatar { get; set; }
    public string? Color { get; set; }
}
