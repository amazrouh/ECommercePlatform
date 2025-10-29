using Core.Enums;

namespace NotificationService.Data;

/// <summary>
/// Repository interface for notification operations.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Gets a notification by its ID.
    /// </summary>
    /// <param name="id">The notification ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The notification entity, or null if not found.</returns>
    Task<NotificationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications by their status.
    /// </summary>
    /// <param name="status">The notification status to filter by.</param>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to take.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of notification entities.</returns>
    Task<IReadOnlyList<NotificationEntity>> GetByStatusAsync(
        NotificationStatus status,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications for a specific recipient.
    /// </summary>
    /// <param name="recipient">The recipient's address.</param>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to take.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of notification entities.</returns>
    Task<IReadOnlyList<NotificationEntity>> GetByRecipientAsync(
        string recipient,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications by type.
    /// </summary>
    /// <param name="type">The notification type to filter by.</param>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to take.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of notification entities.</returns>
    Task<IReadOnlyList<NotificationEntity>> GetByTypeAsync(
        NotificationType type,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new notification.
    /// </summary>
    /// <param name="notification">The notification to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(NotificationEntity notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing notification.
    /// </summary>
    /// <param name="notification">The notification to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(NotificationEntity notification, CancellationToken cancellationToken = default);
}
