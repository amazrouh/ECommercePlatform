using Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NotificationService.Configurations;

namespace NotificationService.HealthChecks;

/// <summary>
/// Health check for the notification service.
/// </summary>
public class NotificationHealthCheck : IHealthCheck
{
    private readonly INotificationService _notificationService;
    private readonly EmailConfig _emailConfig;
    private readonly SmsConfig _smsConfig;
    private readonly PushConfig _pushConfig;

    public NotificationHealthCheck(
        INotificationService notificationService,
        IOptions<EmailConfig> emailConfig,
        IOptions<SmsConfig> smsConfig,
        IOptions<PushConfig> pushConfig)
    {
        _notificationService = notificationService;
        _emailConfig = emailConfig.Value;
        _smsConfig = smsConfig.Value;
        _pushConfig = pushConfig.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var supportedTypes = (await _notificationService.GetSupportedTypes()).ToList();
            if (!supportedTypes.Any())
            {
                return HealthCheckResult.Unhealthy("No notification types are supported.");
            }

            var configurationStatus = ValidateConfigurations();
            if (!configurationStatus.IsValid)
            {
                return HealthCheckResult.Degraded(configurationStatus.Message);
            }

            return HealthCheckResult.Healthy("Notification service is healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Notification service check failed.", ex);
        }
    }

    private (bool IsValid, string Message) ValidateConfigurations()
    {
        // Validate Email Configuration
        if (string.IsNullOrEmpty(_emailConfig.SmtpServer))
        {
            return (false, "Email configuration is incomplete: SMTP server is missing.");
        }

        // Validate SMS Configuration
        if (string.IsNullOrEmpty(_smsConfig.ApiKey))
        {
            return (false, "SMS configuration is incomplete: API key is missing.");
        }

        // Validate Push Configuration
        if (string.IsNullOrEmpty(_pushConfig.FcmServerKey))
        {
            return (false, "Push configuration is incomplete: FCM server key is missing.");
        }

        return (true, "All configurations are valid.");
    }
}
