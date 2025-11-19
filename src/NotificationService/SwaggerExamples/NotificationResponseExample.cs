using NotificationService.DTOs;
using Swashbuckle.AspNetCore.Filters;

namespace NotificationService.SwaggerExamples;

/// <summary>
/// Swagger examples for NotificationResponse
/// </summary>
public class NotificationResponseExample : IMultipleExamplesProvider<NotificationResponse>
{
    /// <summary>
    /// Get multiple examples for NotificationResponse
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SwaggerExample<NotificationResponse>> GetExamples()
    {
        yield return SwaggerExample.Create(
            "Successful Email",
            new NotificationResponse
            {
                MessageId = "EMAIL_abc123def456",
                Type = Core.Enums.NotificationType.Email,
                Recipient = "recipient@example.com",
                Success = true,
                SentAt = DateTimeOffset.UtcNow,
                Error = null
            });

        yield return SwaggerExample.Create(
            "Successful SMS",
            new NotificationResponse
            {
                MessageId = "SMS_789xyz456",
                Type = Core.Enums.NotificationType.Sms,
                Recipient = "+1234567890",
                Success = true,
                SentAt = DateTimeOffset.UtcNow,
                Error = null
            });

        yield return SwaggerExample.Create(
            "Failed Notification",
            new NotificationResponse
            {
                MessageId = null,
                Type = Core.Enums.NotificationType.Email,
                Recipient = "invalid-email",
                Success = false,
                SentAt = DateTimeOffset.UtcNow,
                Error = "Invalid email address format"
            });
    }
}
