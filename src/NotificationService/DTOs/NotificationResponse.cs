using Core.Enums;

namespace NotificationService.DTOs;

/// <summary>
/// Response DTO for notification operations.
/// </summary>
public class NotificationResponse
{
    /// <summary>
    /// Gets or sets whether the notification was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the error message if the notification failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the notification was sent.
    /// </summary>
    public DateTimeOffset SentAt { get; set; }

    /// <summary>
    /// Gets or sets the type of notification that was sent.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets the recipient of the notification.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;
}
