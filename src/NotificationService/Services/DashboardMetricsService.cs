using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Data;
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
    private bool _historicalDataLoaded = false;

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

    private async Task LoadHistoricalDataAsync()
    {
        try
        {
            _logger.LogInformation("Loading historical notification data for dashboard metrics");

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            // Load all notifications from the last 24 hours for metrics calculation
            var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
            var recentNotifications = await repository.GetByStatusAsync(NotificationStatus.Sent, skip: 0, take: 1000);

            var sentNotifications = recentNotifications.Where(n => n.CreatedAt >= yesterday).ToList();

            _logger.LogInformation("Loaded {Count} recent notifications for metrics calculation", sentNotifications.Count);

            // Group by type and update counters
            foreach (var notification in sentNotifications)
            {
                _notificationCounts.AddOrUpdate(notification.Type.ToString(), 1, (_, count) => count + 1);
                _successCounts.AddOrUpdate(notification.Type.ToString(), 1, (_, count) => count + 1);

                // Estimate response time since we don't store it (use average)
                _responseTimes.Add(150.0); // Default estimated response time

                // Update strategy metrics
                _strategyMetrics.AddOrUpdate(notification.Type,
                    new StrategyMetrics
                    {
                        TotalSent = 1,
                        TotalSuccessful = 1,
                        TotalFailed = 0,
                        AverageResponseTimeMs = 150.0,
                        IsActive = true
                    },
                    (_, existing) => new StrategyMetrics
                    {
                        TotalSent = existing.TotalSent + 1,
                        TotalSuccessful = existing.TotalSuccessful + 1,
                        TotalFailed = existing.TotalFailed,
                        AverageResponseTimeMs = (existing.AverageResponseTimeMs * existing.TotalSent + 150.0) / (existing.TotalSent + 1),
                        IsActive = true
                    });
            }

            _logger.LogInformation("Historical data loaded successfully. Email: {EmailCount}, SMS: {SmsCount}, Push: {PushCount}",
                sentNotifications.Count(n => n.Type == NotificationType.Email),
                sentNotifications.Count(n => n.Type == NotificationType.Sms),
                sentNotifications.Count(n => n.Type == NotificationType.Push));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load historical notification data");
            // Don't throw - we can continue without historical data
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dashboard Metrics Service started");

        // Load historical data from database on startup
        if (!_historicalDataLoaded)
        {
            await LoadHistoricalDataAsync();
            _historicalDataLoaded = true;
        }

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
