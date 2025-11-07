using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Hubs;
using NotificationService.Models.Dashboard;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace NotificationService.Services;

/// <summary>
/// Background service for collecting and broadcasting real-time dashboard metrics
/// </summary>
public class DashboardMetricsService : BackgroundService, Core.Interfaces.IMetricsRecorder
{
    private readonly ILogger<DashboardMetricsService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly MessageBatchingService _messageBatcher;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(2);
    private readonly ConcurrentQueue<NotificationEvent> _notificationEvents = new();
    private readonly ConcurrentDictionary<NotificationType, StrategyMetrics> _strategyMetrics = new();

    // Metrics tracking
    private readonly ConcurrentDictionary<string, long> _notificationCounts = new();
    private readonly ConcurrentDictionary<string, long> _successCounts = new();
    private readonly ConcurrentDictionary<string, long> _failureCounts = new();
    private readonly ConcurrentBag<double> _responseTimes = new();
    private readonly ConcurrentQueue<string> _recentErrors = new();

    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;
    private DateTimeOffset _lastMetricsUpdate = DateTimeOffset.UtcNow;

    public DashboardMetricsService(
        ILogger<DashboardMetricsService> logger,
        IHubContext<NotificationHub> hubContext,
        IServiceScopeFactory serviceScopeFactory,
        MessageBatchingService messageBatcher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _serviceScopeFactory = serviceScopeFactory;
        _messageBatcher = messageBatcher;

        // Initialize performance counters
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize performance counters");
        }

        // Initialize strategy metrics
        foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
        {
            _strategyMetrics[type] = new StrategyMetrics { IsActive = true };
            _notificationCounts[type.ToString()] = 0;
            _successCounts[type.ToString()] = 0;
            _failureCounts[type.ToString()] = 0;
        }
    }

    /// <summary>
    /// Record a notification event for metrics collection
    /// </summary>
    public void RecordNotificationEvent(NotificationType type, bool success, double responseTimeMs, string? errorMessage = null, string? userId = null)
    {
        var notificationEvent = new NotificationEvent
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = type,
            Success = success,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage,
            UserId = userId
        };

        _notificationEvents.Enqueue(notificationEvent);

        // Update counters
        _notificationCounts.AddOrUpdate(type.ToString(), 1, (_, count) => count + 1);
        if (success)
        {
            _successCounts.AddOrUpdate(type.ToString(), 1, (_, count) => count + 1);
        }
        else
        {
            _failureCounts.AddOrUpdate(type.ToString(), 1, (_, count) => count + 1);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                _recentErrors.Enqueue($"{DateTimeOffset.UtcNow:HH:mm:ss} - {type}: {errorMessage}");
                // Keep only last 10 errors
                while (_recentErrors.Count > 10 && _recentErrors.TryDequeue(out _)) { }
            }
        }

        // Update response times (keep last 100)
        _responseTimes.Add(responseTimeMs);
        if (_responseTimes.Count > 100)
        {
            var tempList = _responseTimes.ToList();
            tempList.RemoveRange(0, tempList.Count - 100);
            _responseTimes.Clear();
            foreach (var time in tempList)
            {
                _responseTimes.Add(time);
            }
        }

        // Update strategy metrics
        _strategyMetrics.AddOrUpdate(type,
            new StrategyMetrics { TotalSent = 1, TotalSuccessful = success ? 1 : 0, TotalFailed = success ? 0 : 1, AverageResponseTimeMs = responseTimeMs, IsActive = true },
            (_, existing) =>
            {
                existing.TotalSent++;
                if (success) existing.TotalSuccessful++;
                else existing.TotalFailed++;
                existing.AverageResponseTimeMs = (existing.AverageResponseTimeMs + responseTimeMs) / 2;
                return existing;
            });

        // Queue notification event for batched sending
        _messageBatcher.QueueNotificationEvent(notificationEvent);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dashboard Metrics Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAndBroadcastMetricsAsync();
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in metrics collection cycle");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Dashboard Metrics Service stopped");
    }

    private async Task CollectAndBroadcastMetricsAsync()
    {
        var metrics = new DashboardMetrics
        {
            Timestamp = DateTimeOffset.UtcNow,
            ActiveConnections = NotificationHub.GetConnectionCount(),
            ActiveStrategies = _strategyMetrics.Count(kvp => kvp.Value.IsActive),
            StrategyMetrics = new Dictionary<NotificationType, StrategyMetrics>(_strategyMetrics),
            RecentErrors = new List<string>(_recentErrors)
        };

        // Calculate notifications per minute
        var totalNotifications = _notificationCounts.Values.Sum();
        var timeSinceLastUpdate = DateTimeOffset.UtcNow - _lastMetricsUpdate;
        metrics.NotificationsPerMinute = (int)(totalNotifications / Math.Max(timeSinceLastUpdate.TotalMinutes, 1));

        // Calculate success rate
        var totalSuccess = _successCounts.Values.Sum();
        var totalAttempts = totalNotifications;
        metrics.SuccessRate = totalAttempts > 0 ? (double)totalSuccess / totalAttempts * 100 : 0;

        // Calculate average response time
        var responseTimes = _responseTimes.ToArray();
        metrics.AverageResponseTimeMs = responseTimes.Length > 0 ? responseTimes.Average() : 0;

        // Get system metrics
        try
        {
            metrics.CpuUsagePercent = _cpuCounter?.NextValue() ?? 0;
            var totalMemory = 8192; // Assume 8GB total, this should be configurable
            var availableMemory = _memoryCounter?.NextValue() ?? totalMemory;
            metrics.MemoryUsageMB = totalMemory - availableMemory;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect system performance metrics");
        }

        // Queue metrics for batched sending
        _messageBatcher.QueueMetricsUpdate(metrics);

        // Log metrics for audit (using scoped service)
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
            await auditLogger.LogSecurityEventAsync("MetricsCollected", "Real-time metrics collected and broadcasted",
                additionalData: new Dictionary<string, object>
                {
                    ["notifications_per_minute"] = metrics.NotificationsPerMinute,
                    ["success_rate"] = metrics.SuccessRate,
                    ["active_connections"] = metrics.ActiveConnections
                });
        }

        _lastMetricsUpdate = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Get current metrics snapshot
    /// </summary>
    public DashboardMetrics GetCurrentMetrics()
    {
        var totalNotifications = _notificationCounts.Values.Sum();
        var totalSuccess = _successCounts.Values.Sum();
        var responseTimes = _responseTimes.ToArray();

        return new DashboardMetrics
        {
            Timestamp = DateTimeOffset.UtcNow,
            ActiveConnections = NotificationHub.GetConnectionCount(),
            ActiveStrategies = _strategyMetrics.Count(kvp => kvp.Value.IsActive),
            NotificationsPerMinute = (int)(totalNotifications / Math.Max((DateTimeOffset.UtcNow - _lastMetricsUpdate).TotalMinutes, 1)),
            SuccessRate = totalNotifications > 0 ? (double)totalSuccess / totalNotifications * 100 : 0,
            AverageResponseTimeMs = responseTimes.Length > 0 ? responseTimes.Average() : 0,
            StrategyMetrics = new Dictionary<NotificationType, StrategyMetrics>(_strategyMetrics),
            RecentErrors = new List<string>(_recentErrors)
        };
    }

    /// <summary>
    /// Get strategy metrics for a specific type
    /// </summary>
    public StrategyMetrics? GetStrategyMetrics(NotificationType type)
    {
        return _strategyMetrics.TryGetValue(type, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Reset all metrics (useful for testing or manual reset)
    /// </summary>
    public void ResetMetrics()
    {
        foreach (var key in _notificationCounts.Keys)
        {
            _notificationCounts[key] = 0;
            _successCounts[key] = 0;
            _failureCounts[key] = 0;
        }

        _responseTimes.Clear();
        while (_recentErrors.TryDequeue(out _)) { }

        foreach (var type in _strategyMetrics.Keys)
        {
            _strategyMetrics[type] = new StrategyMetrics { IsActive = true };
        }

        _lastMetricsUpdate = DateTimeOffset.UtcNow;
        _logger.LogInformation("Metrics reset completed");
    }
}
