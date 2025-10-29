using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Configurations;
using NotificationService.Validators;

namespace NotificationService.Strategies;

/// <summary>
/// Strategy for sending SMS notifications.
/// </summary>
public class SmsNotificationStrategy : INotificationStrategy
{
    private readonly ILogger<SmsNotificationStrategy> _logger;
    private readonly SmsConfig _config;
    private readonly SmsMessageValidator _validator;

    public NotificationType Type => NotificationType.Sms;

    public SmsNotificationStrategy(
        ILogger<SmsNotificationStrategy> logger,
        IOptions<SmsConfig> config,
        SmsMessageValidator validator)
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
                _logger.LogError("SMS validation failed: {Errors}", errors);
                return NotificationResult.Failed(errors);
            }

            _logger.LogInformation("Sending SMS to {PhoneNumber}", message.To);

            // Handle long messages if configured
            if (message.Body.Length > _config.MaxMessageLength && !_config.SplitLongMessages)
            {
                var error = $"Message exceeds maximum length of {_config.MaxMessageLength} characters";
                _logger.LogError(error);
                return NotificationResult.Failed(error);
            }

            // Simulate SMS sending delay
            await Task.Delay(200, cancellationToken);

            // In a real implementation, this would use Twilio, MessageBird, or similar SDK
            var messageId = $"SMS_{Guid.NewGuid():N}";

            _logger.LogInformation("SMS sent successfully. MessageId: {MessageId}", messageId);
            return NotificationResult.Succeeded(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", message.To);
            return NotificationResult.Failed(ex.Message);
        }
    }
}
