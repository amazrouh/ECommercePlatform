using Core.Enums;
using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// High-level service for sending notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification using the appropriate strategy for the specified type.
    /// </summary>
    /// <param name="type">The type of notification to send.</param>
    /// <param name="message">The notification message to send.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A NotificationResult indicating the success or failure of the operation.</returns>
    Task<NotificationResult> SendAsync(NotificationType type, NotificationMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple notifications in parallel using appropriate strategies.
    /// </summary>
    /// <param name="notifications">A dictionary mapping notification types to their messages.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A dictionary mapping notification types to their results.</returns>
    Task<IDictionary<NotificationType, NotificationResult>> SendBatchAsync(
        IDictionary<NotificationType, NotificationMessage> notifications,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the supported notification types.
    /// </summary>
    /// <returns>An enumerable of supported notification types.</returns>
    Task<IEnumerable<NotificationType>> GetSupportedTypes();
}
