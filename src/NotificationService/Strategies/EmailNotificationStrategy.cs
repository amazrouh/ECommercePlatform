using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Configurations;
using NotificationService.Validators;

namespace NotificationService.Strategies;

/// <summary>
/// Strategy for sending email notifications.
/// </summary>
public class EmailNotificationStrategy : INotificationStrategy
{
    private readonly ILogger<EmailNotificationStrategy> _logger;
    private readonly EmailConfig _config;
    private readonly EmailMessageValidator _validator;

    public NotificationType Type => NotificationType.Email;

    public EmailNotificationStrategy(
        ILogger<EmailNotificationStrategy> logger,
        IOptions<EmailConfig> config,
        EmailMessageValidator validator)
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
                _logger.LogError("Email validation failed: {Errors}", errors);
                return NotificationResult.Failed(errors);
            }

            _logger.LogInformation("Sending email to {Recipient} with subject: {Subject}", message.To, message.Subject);

            // Simulate email sending delay
            await Task.Delay(100, cancellationToken);

            // In a real implementation, this would use SmtpClient or a mail service SDK
            var messageId = $"EMAIL_{Guid.NewGuid():N}";

            _logger.LogInformation("Email sent successfully. MessageId: {MessageId}", messageId);
            return NotificationResult.Succeeded(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", message.To);
            return NotificationResult.Failed(ex.Message);
        }
    }
}
