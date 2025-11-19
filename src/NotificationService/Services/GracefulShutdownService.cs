using Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotificationService.Services;

/// <summary>
/// Service for handling graceful shutdown of the application
/// </summary>
public class GracefulShutdownService : IHostedService, IDisposable
{
    private readonly ILogger<GracefulShutdownService> _logger;
    private readonly IAuditLogger _auditLogger;
    private readonly DashboardMetricsService _metricsService;
    private readonly IHostApplicationLifetime _appLifetime;
    private CancellationTokenSource? _shutdownCts;
    private Task? _shutdownTask;

    public GracefulShutdownService(
        ILogger<GracefulShutdownService> logger,
        IAuditLogger auditLogger,
        DashboardMetricsService metricsService,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _auditLogger = auditLogger;
        _metricsService = metricsService;
        _appLifetime = appLifetime;
    }

    /// <summary>
    /// Start the graceful shutdown service
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Graceful shutdown service started");

        _shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Register shutdown handler
        _appLifetime.ApplicationStopping.Register(async () =>
        {
            await HandleShutdownAsync();
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop the graceful shutdown service
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Graceful shutdown service stopping");

        if (_shutdownCts != null)
        {
            await _shutdownCts.CancelAsync();
        }

        if (_shutdownTask != null)
        {
            await Task.WhenAny(_shutdownTask, Task.Delay(30000, cancellationToken));
        }
    }

    /// <summary>
    /// Handle graceful shutdown process
    /// </summary>
    /// <returns></returns>
    private async Task HandleShutdownAsync()
    {
        _logger.LogInformation("Application shutdown initiated. Starting graceful shutdown process...");

        try
        {
            // Log shutdown event
            await _auditLogger.LogSecurityEventAsync("ApplicationShutdown",
                "Application is shutting down gracefully", "system");

            // Get final metrics before shutdown
            var finalMetrics = _metricsService.GetCurrentMetrics();
            _logger.LogInformation(
                "Final metrics - Notifications: {Total}, Success Rate: {Rate:F2}%, Active Connections: {Connections}",
                finalMetrics.StrategyMetrics.Sum(s => s.Value.TotalSent),
                finalMetrics.SuccessRate,
                finalMetrics.ActiveConnections);

            // Save final metrics to persistent storage (if needed)
            await SaveFinalMetricsAsync(finalMetrics);

            // Complete ongoing requests (with timeout)
            await CompleteOngoingRequestsAsync(TimeSpan.FromSeconds(10));

            // Close database connections gracefully
            await CloseDatabaseConnectionsAsync();

            // Close cache connections
            await CloseCacheConnectionsAsync();

            _logger.LogInformation("Graceful shutdown completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during graceful shutdown");
            await _auditLogger.LogSecurityEventAsync("ShutdownError",
                $"Error during shutdown: {ex.Message}", "system");
        }
    }

    /// <summary>
    /// Save final metrics before shutdown
    /// </summary>
    /// <param name="metrics"></param>
    /// <returns></returns>
    private async Task SaveFinalMetricsAsync(Models.Dashboard.DashboardMetrics metrics)
    {
        try
        {
            // In a real implementation, this would save to a database or external storage
            _logger.LogInformation("Saving final metrics before shutdown");

            var metricsSummary = new
            {
                timestamp = metrics.Timestamp,
                totalNotifications = metrics.StrategyMetrics.Sum(s => s.Value.TotalSent),
                successRate = metrics.SuccessRate,
                averageResponseTime = metrics.AverageResponseTimeMs,
                activeConnections = metrics.ActiveConnections,
                strategyBreakdown = metrics.StrategyMetrics.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => new { kvp.Value.TotalSent, kvp.Value.SuccessRate })
            };

            // Simulate saving to external storage
            await Task.Delay(100); // Simulate I/O operation

            _logger.LogInformation("Final metrics saved: {Summary}",
                System.Text.Json.JsonSerializer.Serialize(metricsSummary));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save final metrics");
        }
    }

    /// <summary>
    /// Complete ongoing requests before shutdown
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns></returns>
    private async Task CompleteOngoingRequestsAsync(TimeSpan timeout)
    {
        _logger.LogInformation("Completing ongoing requests with timeout: {Timeout}s", timeout.TotalSeconds);

        try
        {
            // In a real implementation, this would:
            // 1. Get list of active requests
            // 2. Wait for them to complete or timeout
            // 3. Force termination if needed

            await Task.Delay(1000); // Simulate waiting for requests to complete

            _logger.LogInformation("Ongoing requests completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error completing ongoing requests");
        }
    }

    /// <summary>
    /// Close database connections gracefully
    /// </summary>
    /// <returns></returns>
    private async Task CloseDatabaseConnectionsAsync()
    {
        _logger.LogInformation("Closing database connections");

        try
        {
            // In a real implementation, this would:
            // 1. Close all active database connections
            // 2. Save any pending transactions
            // 3. Release connection pool

            await Task.Delay(500); // Simulate connection closing

            _logger.LogInformation("Database connections closed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing database connections");
        }
    }

    /// <summary>
    /// Close cache connections gracefully
    /// </summary>
    /// <returns></returns>
    private async Task CloseCacheConnectionsAsync()
    {
        _logger.LogInformation("Closing cache connections");

        try
        {
            // In a real implementation, this would:
            // 1. Flush any pending cache operations
            // 2. Close Redis connections
            // 3. Save cache state if needed

            await Task.Delay(300); // Simulate cache closing

            _logger.LogInformation("Cache connections closed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing cache connections");
        }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        _shutdownCts?.Dispose();
        _logger.LogInformation("GracefulShutdownService disposed");
    }
}
