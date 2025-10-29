using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace NotificationService.DTOs;

/// <summary>
/// Request DTO for sending a notification.
/// </summary>
public class SendNotificationRequest
{
    /// <summary>
    /// Gets or sets the type of notification to send.
    /// </summary>
    [Required]
    public NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets the recipient of the notification.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject of the notification.
    /// </summary>
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the body content of the notification.
    /// </summary>
    [Required]
    [StringLength(100000)]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata for the notification.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
