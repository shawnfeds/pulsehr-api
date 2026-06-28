using System.Net;
using System.Text.Json;
using PulseHR.Api.DTOs.Common;

namespace PulseHR.Api.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = ex switch
        {
            ArgumentException or ArgumentNullException => (HttpStatusCode.BadRequest, ex.Message),
            UnauthorizedAccessException                => (HttpStatusCode.Unauthorized, "Unauthorized."),
            KeyNotFoundException                       => (HttpStatusCode.NotFound, ex.Message),
            InvalidOperationException                  => (HttpStatusCode.BadRequest, ex.Message),
            _                                          => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
