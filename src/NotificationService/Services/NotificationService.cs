using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace NotificationService.Services;

/// <summary>
/// Service for sending notifications using appropriate strategies.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationStrategyFactory _strategyFactory;
    private readonly ILogger<NotificationService> _logger;
    private readonly IAuditLogger _auditLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Core.Interfaces.IMetricsRecorder _metricsRecorder;
    private readonly Data.INotificationRepository _notificationRepository;

    public NotificationService(
        INotificationStrategyFactory strategyFactory,
        ILogger<NotificationService> logger,
        IAuditLogger auditLogger,
        IHttpContextAccessor httpContextAccessor,
        Core.Interfaces.IMetricsRecorder metricsRecorder,
        Data.INotificationRepository notificationRepository)
    {
        _strategyFactory = strategyFactory;
        _logger = logger;
        _auditLogger = auditLogger;
        _httpContextAccessor = httpContextAccessor;
        _metricsRecorder = metricsRecorder;
        _notificationRepository = notificationRepository;
    }

    /// <inheritdoc />
    public async Task<NotificationResult> SendAsync(
        NotificationType type,
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            _logger.LogInformation("Sending {Type} notification to {Recipient}", type, message.To);

            var strategy = _strategyFactory.GetStrategy(type);
            var result = await strategy.SendAsync(message, cancellationToken);
            var responseTime = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            if (result.Success)
            {
                _logger.LogInformation(
                    "{Type} notification sent successfully. MessageId: {MessageId}",
                    type, result.MessageId);

                // Record metrics
                _metricsRecorder.RecordNotificationEvent(type, true, responseTime, null, userId);

                // Audit log successful notification
                await _auditLogger.LogNotificationSentAsync(message, result, userId);
            }
            else
            {
                _logger.LogError(
                    "{Type} notification failed. Error: {Error}",
                    type, result.Error);

                // Record metrics
                _metricsRecorder.RecordNotificationEvent(type, false, responseTime, result.Error, userId);

                // Audit log failed notification
                await _auditLogger.LogNotificationFailedAsync(message, result.Error ?? "Unknown error", userId);
            }

            return result;
        }
        catch (Exception ex)
        {
            var responseTime = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            _logger.LogError(ex,
                "Failed to send {Type} notification to {Recipient}",
                type, message.To);

            // Record metrics
            _metricsRecorder.RecordNotificationEvent(type, false, responseTime, ex.Message, userId);

            // Audit log failed notification
            await _auditLogger.LogNotificationFailedAsync(message, ex.Message, userId);

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

    private string? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst(ClaimTypes.Name)?.Value;
    }
}
