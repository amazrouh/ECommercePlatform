using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Interface for audit logging operations
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs a successful notification send operation
    /// </summary>
    /// <param name="message">The notification message that was sent</param>
    /// <param name="result">The result of the send operation</param>
    /// <param name="userId">The user ID who initiated the operation</param>
    Task LogNotificationSentAsync(NotificationMessage message, NotificationResult result, string? userId = null);

    /// <summary>
    /// Logs a failed notification send operation
    /// </summary>
    /// <param name="message">The notification message that failed to send</param>
    /// <param name="error">The error message</param>
    /// <param name="userId">The user ID who initiated the operation</param>
    Task LogNotificationFailedAsync(NotificationMessage message, string error, string? userId = null);

    /// <summary>
    /// Logs authentication failure
    /// </summary>
    /// <param name="username">The username that failed authentication</param>
    /// <param name="ipAddress">The IP address of the request</param>
    /// <param name="reason">The reason for authentication failure</param>
    Task LogAuthenticationFailureAsync(string username, string ipAddress, string reason);

    /// <summary>
    /// Logs successful authentication
    /// </summary>
    /// <param name="username">The username that was authenticated</param>
    /// <param name="ipAddress">The IP address of the request</param>
    Task LogAuthenticationSuccessAsync(string username, string ipAddress);

    /// <summary>
    /// Logs authorization failure
    /// </summary>
    /// <param name="userId">The user ID that failed authorization</param>
    /// <param name="resource">The resource being accessed</param>
    /// <param name="action">The action being performed</param>
    /// <param name="ipAddress">The IP address of the request</param>
    Task LogAuthorizationFailureAsync(string userId, string resource, string action, string ipAddress);

    /// <summary>
    /// Logs a security event
    /// </summary>
    /// <param name="eventType">The type of security event</param>
    /// <param name="description">Description of the event</param>
    /// <param name="userId">The user ID associated with the event (optional)</param>
    /// <param name="ipAddress">The IP address of the request (optional)</param>
    /// <param name="additionalData">Additional data related to the event</param>
    Task LogSecurityEventAsync(string eventType, string description, string? userId = null, string? ipAddress = null, IDictionary<string, object>? additionalData = null);
}
