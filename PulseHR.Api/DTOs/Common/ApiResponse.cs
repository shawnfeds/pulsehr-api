namespace PulseHR.Api.DTOs.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }

    public static ApiResponse<T> Ok(T data) =>
        new() { Success = true, Data = data };

    public static ApiResponse<object> Fail(string message) =>
        new ApiResponse<object> { Success = false, Message = message };

    public static ApiResponse<T> SuccessMessage(string message) =>
        new() { Success = true, Message = message };
}
