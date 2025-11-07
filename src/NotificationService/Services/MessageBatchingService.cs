using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NotificationService.Hubs;
using NotificationService.Models.Dashboard;
using System.Collections.Concurrent;
using System.Threading;

namespace NotificationService.Services;

/// <summary>
/// Service for batching and optimizing SignalR messages
/// </summary>
public class MessageBatchingService : IDisposable
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<MessageBatchingService> _logger;
    private readonly ConcurrentQueue<RealTimeUpdate> _messageQueue = new();
    private readonly Timer _batchTimer;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    private readonly TimeSpan _batchInterval = TimeSpan.FromMilliseconds(100); // Batch every 100ms
    private bool _disposed;

    public MessageBatchingService(
        IHubContext<NotificationHub> hubContext,
        ILogger<MessageBatchingService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        _batchTimer = new Timer(ProcessBatch, null, _batchInterval, _batchInterval);
    }

    /// <summary>
    /// Queues a message for batched sending
    /// </summary>
    public void QueueMessage(RealTimeUpdate update)
    {
        _messageQueue.Enqueue(update);
    }

    /// <summary>
    /// Sends a message immediately without batching
    /// </summary>
    public async Task SendImmediateAsync(RealTimeUpdate update)
    {
        await _hubContext.SendUpdateToDashboard(update);
    }

    /// <summary>
    /// Forces processing of current batch
    /// </summary>
    public async Task FlushBatchAsync()
    {
        await ProcessBatchAsync();
    }

    private async void ProcessBatch(object? state)
    {
        await ProcessBatchAsync();
    }

    private async Task ProcessBatchAsync()
    {
        if (!await _processingSemaphore.WaitAsync(0))
        {
            // Another batch is being processed
            return;
        }

        try
        {
            var batch = new List<RealTimeUpdate>();

            // Dequeue all available messages
            while (_messageQueue.TryDequeue(out var message))
            {
                batch.Add(message);
            }

            if (batch.Count == 0)
            {
                return;
            }

            // Group messages by type for optimization
            var groupedMessages = batch.GroupBy(m => m.Type);

            foreach (var group in groupedMessages)
            {
                switch (group.Key)
                {
                    case UpdateType.Metrics:
                        // Send only the latest metrics update
                        var latestMetrics = group.OrderByDescending(m => m.Metrics?.Timestamp).First();
                        await _hubContext.SendMetricsToDashboard(latestMetrics.Metrics!);
                        break;

                    case UpdateType.NotificationEvent:
                        // Batch notification events
                        if (group.Count() == 1)
                        {
                            await _hubContext.SendNotificationEventToDashboard(group.First().NotificationEvent!);
                        }
                        else
                        {
                            // For multiple events, send as batch
                            var batchUpdate = new RealTimeUpdate
                            {
                                Type = UpdateType.NotificationEvent,
                                NotificationEvent = new NotificationEvent
                                {
                                    Timestamp = DateTimeOffset.UtcNow,
                                    Type = Core.Enums.NotificationType.Email, // Default type for batch
                                    Success = true,
                                    ResponseTimeMs = 0,
                                    ErrorMessage = $"{group.Count()} notification events batched"
                                }
                            };
                            await _hubContext.SendUpdateToDashboard(batchUpdate);
                        }
                        break;

                    case UpdateType.HealthEvent:
                        // Send all health events (they're important)
                        foreach (var healthUpdate in group)
                        {
                            await _hubContext.SendHealthEventToDashboard(healthUpdate.HealthEvent!);
                        }
                        break;

                    case UpdateType.ConnectionUpdate:
                        // Send only the latest connection update
                        var latestConnection = group.OrderByDescending(m => m.Metrics?.Timestamp).First();
                        await _hubContext.SendUpdateToDashboard(latestConnection);
                        break;
                }
            }

            if (batch.Count > 1)
            {
                _logger.LogDebug("Processed batch of {Count} messages", batch.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message batch");
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _batchTimer?.Dispose();
            _processingSemaphore?.Dispose();
        }

        _disposed = true;
    }
}

/// <summary>
/// Extension methods for message batching
/// </summary>
public static class MessageBatchingExtensions
{
    /// <summary>
    /// Queues a metrics update for batched sending
    /// </summary>
    public static void QueueMetricsUpdate(this MessageBatchingService batcher, DashboardMetrics metrics)
    {
        var update = new RealTimeUpdate
        {
            Type = UpdateType.Metrics,
            Metrics = metrics
        };
        batcher.QueueMessage(update);
    }

    /// <summary>
    /// Queues a notification event for batched sending
    /// </summary>
    public static void QueueNotificationEvent(this MessageBatchingService batcher, NotificationEvent notificationEvent)
    {
        var update = new RealTimeUpdate
        {
            Type = UpdateType.NotificationEvent,
            NotificationEvent = notificationEvent
        };
        batcher.QueueMessage(update);
    }

    /// <summary>
    /// Queues a health event for batched sending
    /// </summary>
    public static void QueueHealthEvent(this MessageBatchingService batcher, HealthEvent healthEvent)
    {
        var update = new RealTimeUpdate
        {
            Type = UpdateType.HealthEvent,
            HealthEvent = healthEvent
        };
        batcher.QueueMessage(update);
    }
}


