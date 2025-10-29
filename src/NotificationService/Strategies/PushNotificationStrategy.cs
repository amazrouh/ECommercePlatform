using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Configurations;
using NotificationService.Validators;
using System.Text.Json;

namespace NotificationService.Strategies;

/// <summary>
/// Strategy for sending push notifications.
/// </summary>
public class PushNotificationStrategy : INotificationStrategy
{
    private readonly ILogger<PushNotificationStrategy> _logger;
    private readonly PushConfig _config;
    private readonly PushMessageValidator _validator;

    public NotificationType Type => NotificationType.Push;

    public PushNotificationStrategy(
        ILogger<PushNotificationStrategy> logger,
        IOptions<PushConfig> config,
        PushMessageValidator validator)
    {
        _logger = logger;
        _config = config.Value;
        _validator = validator;
    }

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(message, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogError("Push notification validation failed: {Errors}", errors);
                return NotificationResult.Failed(errors);
            }

            _logger.LogInformation("Sending push notification to device: {DeviceToken}", message.To);

            // Build notification payload
            var payload = new
            {
                message.Subject,
                message.Body,
                data = message.Metadata,
                ttl = _config.DefaultTtl
            };

            // Check payload size
            var payloadJson = JsonSerializer.Serialize(payload);
            if (payloadJson.Length > _config.MaxPayloadSize)
            {
                var error = $"Payload size ({payloadJson.Length} bytes) exceeds maximum allowed size ({_config.MaxPayloadSize} bytes)";
                _logger.LogError(error);
                return NotificationResult.Failed(error);
            }

            // Simulate push notification delay
            await Task.Delay(150, cancellationToken);

            // In a real implementation, this would use FCM, APNS, or similar SDK
            var messageId = $"PUSH_{Guid.NewGuid():N}";

            _logger.LogInformation("Push notification sent successfully. MessageId: {MessageId}", messageId);
            return NotificationResult.Succeeded(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to device: {DeviceToken}", message.To);
            return NotificationResult.Failed(ex.Message);
        }
    }
}
