using System.Net;
using System.Text.Json;

namespace NotificationService.Middleware;

/// <summary>
/// Middleware for handling exceptions globally.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid argument provided."),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid operation attempted."),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        response.StatusCode = (int)statusCode;

        var result = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            message = message,
            detail = _environment.IsDevelopment() ? exception.ToString() : null
        });

        return response.WriteAsync(result);
    }
}
