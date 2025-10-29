using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace NotificationService.Decorators;

/// <summary>
/// Decorator that adds logging to notification operations.
/// </summary>
public class LoggingNotificationDecorator : INotificationService
{
    private readonly INotificationService _inner;
    private readonly ILogger<LoggingNotificationDecorator> _logger;

    public LoggingNotificationDecorator(
        INotificationService inner,
        ILogger<LoggingNotificationDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<NotificationResult> SendAsync(
        NotificationType type,
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Starting to send {Type} notification to {Recipient}",
                type, message.To);

            var startTime = DateTimeOffset.UtcNow;
            var result = await _inner.SendAsync(type, message, cancellationToken);
            var duration = DateTimeOffset.UtcNow - startTime;

            if (result.Success)
            {
                _logger.LogInformation(
                    "Successfully sent {Type} notification to {Recipient} in {Duration}ms. MessageId: {MessageId}",
                    type, message.To, duration.TotalMilliseconds, result.MessageId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send {Type} notification to {Recipient} after {Duration}ms. Error: {Error}",
                    type, message.To, duration.TotalMilliseconds, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending {Type} notification to {Recipient}",
                type, message.To);
            throw;
        }
    }

    public async Task<IDictionary<NotificationType, NotificationResult>> SendBatchAsync(
        IDictionary<NotificationType, NotificationMessage> notifications,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Starting to send batch of {Count} notifications",
                notifications.Count);

            var startTime = DateTimeOffset.UtcNow;
            var results = await _inner.SendBatchAsync(notifications, cancellationToken);
            var duration = DateTimeOffset.UtcNow - startTime;

            var successCount = results.Count(r => r.Value.Success);
            var failureCount = results.Count - successCount;

            _logger.LogInformation(
                "Completed batch send in {Duration}ms. Success: {SuccessCount}, Failed: {FailureCount}",
                duration.TotalMilliseconds, successCount, failureCount);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending batch of {Count} notifications",
                notifications.Count);
            throw;
        }
    }

    public Task<IEnumerable<NotificationType>> GetSupportedTypes()
        => _inner.GetSupportedTypes();
}
