using Core.Common;
using Core.Enums;

namespace NotificationService.Data;

/// <summary>
/// Represents a notification entity in the database.
/// </summary>
public class NotificationEntity : Entity, IAggregateRoot
{
    /// <summary>
    /// Gets or sets the type of notification.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets the recipient of the notification.
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject of the notification.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the body content of the notification.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metadata associated with the notification.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the status of the notification.
    /// </summary>
    public NotificationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the notification was sent.
    /// </summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// Gets or sets any error message if the notification failed.
    /// </summary>
    public string? Error { get; set; }
}
