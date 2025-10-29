using Core.Enums;
using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Defines a strategy for sending notifications of a specific type.
/// </summary>
public interface INotificationStrategy
{
    /// <summary>
    /// Gets the type of notification this strategy handles.
    /// </summary>
    NotificationType Type { get; }

    /// <summary>
    /// Sends a notification using this strategy.
    /// </summary>
    /// <param name="message">The notification message to send.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A NotificationResult indicating the success or failure of the operation.</returns>
    Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
