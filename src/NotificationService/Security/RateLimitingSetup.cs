using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using NotificationService.Configurations;
using System.Threading.RateLimiting;

namespace NotificationService.Security;

/// <summary>
/// Setup class for configuring rate limiting
/// </summary>
public static class RateLimitingSetup
{
    /// <summary>
    /// Configures rate limiting for the application
    /// </summary>
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitConfig = configuration.GetSection("Security:RateLimit").Get<RateLimitConfig>() ?? new RateLimitConfig();

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Rate limit by IP address
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = rateLimitConfig.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitConfig.WindowSeconds)
                    });
            });

            // Configure rate limit exceeded response
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var response = new
                {
                    error = "Too many requests",
                    message = $"Rate limit exceeded. Maximum {rateLimitConfig.PermitLimit} requests per {rateLimitConfig.WindowSeconds} seconds.",
                    retryAfter = context.HttpContext.Response.Headers.RetryAfter.ToString()
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, token);
            };
        });

        return services;
    }
}
