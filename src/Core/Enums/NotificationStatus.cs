namespace Core.Enums;

/// <summary>
/// Represents the current status of a notification.
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification is pending to be sent.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Notification has been sent to the provider.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Notification failed to be sent.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Notification has been successfully delivered to the recipient.
    /// </summary>
    Delivered = 4
}
