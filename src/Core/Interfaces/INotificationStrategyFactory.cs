using Core.Enums;

namespace Core.Interfaces;

/// <summary>
/// Factory for creating notification strategy instances.
/// </summary>
public interface INotificationStrategyFactory
{
    /// <summary>
    /// Gets a notification strategy for the specified notification type.
    /// </summary>
    /// <param name="type">The type of notification to get a strategy for.</param>
    /// <returns>A strategy instance that can handle the specified notification type.</returns>
    /// <exception cref="ArgumentException">Thrown when no strategy is available for the specified type.</exception>
    INotificationStrategy GetStrategy(NotificationType type);
}
