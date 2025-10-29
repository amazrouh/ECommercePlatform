using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace NotificationService.Services;

/// <summary>
/// Service for sending notifications using appropriate strategies.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationStrategyFactory _strategyFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationStrategyFactory strategyFactory,
        ILogger<NotificationService> logger)
    {
        _strategyFactory = strategyFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<NotificationResult> SendAsync(
        NotificationType type,
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending {Type} notification to {Recipient}", type, message.To);

            var strategy = _strategyFactory.GetStrategy(type);
            var result = await strategy.SendAsync(message, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "{Type} notification sent successfully. MessageId: {MessageId}",
                    type, result.MessageId);
            }
            else
            {
                _logger.LogError(
                    "{Type} notification failed. Error: {Error}",
                    type, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send {Type} notification to {Recipient}",
                type, message.To);
            return NotificationResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<IDictionary<NotificationType, NotificationResult>> SendBatchAsync(
        IDictionary<NotificationType, NotificationMessage> notifications,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending batch of {Count} notifications", notifications.Count);

        var tasks = notifications.Select(async notification =>
        {
            var result = await SendAsync(notification.Key, notification.Value, cancellationToken);
            return new KeyValuePair<NotificationType, NotificationResult>(notification.Key, result);
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(x => x.Key, x => x.Value);
    }

    /// <inheritdoc />
    public Task<IEnumerable<NotificationType>> GetSupportedTypes()
        => Task.FromResult(NotificationStrategyFactory.GetSupportedTypes());
}
