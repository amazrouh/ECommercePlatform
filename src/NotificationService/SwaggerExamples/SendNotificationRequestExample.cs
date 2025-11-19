using NotificationService.DTOs;
using Swashbuckle.AspNetCore.Filters;

namespace NotificationService.SwaggerExamples;

/// <summary>
/// Swagger examples for SendNotificationRequest
/// </summary>
public class SendNotificationRequestExample : IMultipleExamplesProvider<SendNotificationRequest>
{
    /// <summary>
    /// Get multiple examples for SendNotificationRequest
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SwaggerExample<SendNotificationRequest>> GetExamples()
    {
        yield return SwaggerExample.Create(
            "Email Notification",
            new SendNotificationRequest
            {
                Type = Core.Enums.NotificationType.Email,
                To = "user@example.com",
                Subject = "Welcome to our platform!",
                Body = "Thank you for joining us. Your account has been successfully created."
            });

        yield return SwaggerExample.Create(
            "SMS Notification",
            new SendNotificationRequest
            {
                Type = Core.Enums.NotificationType.Sms,
                To = "+1234567890",
                Subject = "",
                Body = "Your verification code is: 123456"
            });

        yield return SwaggerExample.Create(
            "Push Notification",
            new SendNotificationRequest
            {
                Type = Core.Enums.NotificationType.Push,
                To = "device-token-abc123",
                Subject = "New Message",
                Body = "You have received a new message from John Doe"
            });

        yield return SwaggerExample.Create(
            "Webhook Notification",
            new SendNotificationRequest
            {
                Type = Core.Enums.NotificationType.Webhook,
                To = "https://api.example.com/webhooks/notifications",
                Subject = "Order Status Update",
                Body = "{\"orderId\": \"12345\", \"status\": \"shipped\", \"trackingNumber\": \"1Z999AA1234567890\"}"
            });
    }
}
