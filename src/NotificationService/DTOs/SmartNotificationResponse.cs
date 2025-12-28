using Core.Models;

namespace NotificationService.DTOs;

/// <summary>
/// Response for AI-enhanced notification sending with analysis insights
/// </summary>
public class SmartNotificationResponse
{
    /// <summary>
    /// Whether the notification was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Unique message identifier from the notification provider
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Error message if the notification failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The type of notification that was sent
    /// </summary>
    public Core.Enums.NotificationType NotificationType { get; set; }

    /// <summary>
    /// The recipient of the notification
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// AI analysis results for the notification content
    /// </summary>
    public AIAnalysisResult AIAnalysis { get; set; } = new();
}
