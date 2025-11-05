using Microsoft.Extensions.Options;
using NotificationService.Configurations;

namespace NotificationService.Middleware;

/// <summary>
/// Middleware for adding security headers to HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersConfig _config;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityConfig> securityConfig)
    {
        _next = next;
        _config = securityConfig.Value.SecurityHeaders;
    }

    /// <summary>
    /// Processes the HTTP request and adds security headers to the response
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        context.Response.Headers["X-Content-Type-Options"] = _config.ContentTypeOptions;
        context.Response.Headers["X-Frame-Options"] = _config.FrameOptions;
        context.Response.Headers["Referrer-Policy"] = _config.ReferrerPolicy;
        context.Response.Headers["Content-Security-Policy"] = _config.ContentSecurityPolicy;

        // Add additional security headers
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        await _next(context);
    }
}

/// <summary>
/// Extension methods for adding security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds the security headers middleware to the request pipeline
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
