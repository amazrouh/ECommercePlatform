using Core.Enums;

namespace Core.Interfaces;

/// <summary>
/// Interface for recording notification metrics
/// </summary>
public interface IMetricsRecorder
{
    /// <summary>
    /// Record a notification event for metrics collection
    /// </summary>
    /// <param name="type">Notification type</param>
    /// <param name="success">Whether the notification was successful</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    /// <param name="errorMessage">Error message if failed</param>
    /// <param name="userId">User ID who initiated the notification</param>
    void RecordNotificationEvent(NotificationType type, bool success, double responseTimeMs, string? errorMessage = null, string? userId = null);
}

/// <summary>
/// Null implementation for when metrics are disabled
/// </summary>
public class NullMetricsRecorder : IMetricsRecorder
{
    public void RecordNotificationEvent(NotificationType type, bool success, double responseTimeMs, string? errorMessage = null, string? userId = null)
    {
        // Do nothing
    }
}


