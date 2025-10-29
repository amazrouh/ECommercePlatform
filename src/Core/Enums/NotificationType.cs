namespace Core.Enums;

/// <summary>
/// Represents the type of notification to be sent.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Email notification type.
    /// </summary>
    Email = 1,

    /// <summary>
    /// SMS (text message) notification type.
    /// </summary>
    Sms = 2,

    /// <summary>
    /// Push notification type (mobile/web).
    /// </summary>
    Push = 3,

    /// <summary>
    /// Webhook notification type (HTTP callbacks).
    /// </summary>
    Webhook = 4
}
