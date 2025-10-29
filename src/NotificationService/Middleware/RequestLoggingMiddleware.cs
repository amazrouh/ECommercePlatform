using System.Diagnostics;

namespace NotificationService.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        try
        {
            // Log the request
            _logger.LogInformation(
                "Request {RequestId} {Method} {Path} started",
                requestId, context.Request.Method, context.Request.Path);

            // Call the next middleware
            await _next(context);

            stopwatch.Stop();

            // Log the response
            _logger.LogInformation(
                "Request {RequestId} {Method} {Path} completed with status {StatusCode} in {ElapsedMs}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            stopwatch.Stop();

            _logger.LogError(
                "Request {RequestId} {Method} {Path} failed after {ElapsedMs}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
