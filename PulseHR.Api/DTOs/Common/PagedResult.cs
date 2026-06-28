namespace PulseHR.Api.DTOs.Common;

public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = [];
    public int Total { get; set; }
}
