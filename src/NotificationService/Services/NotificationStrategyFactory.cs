using Core.Enums;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Strategies;

namespace NotificationService.Services;

/// <summary>
/// Factory for creating notification strategy instances.
/// </summary>
public class NotificationStrategyFactory : INotificationStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly Dictionary<NotificationType, Type> _strategyMap;

    static NotificationStrategyFactory()
    {
        _strategyMap = new Dictionary<NotificationType, Type>
        {
            { NotificationType.Email, typeof(EmailNotificationStrategy) },
            { NotificationType.Sms, typeof(SmsNotificationStrategy) },
            { NotificationType.Push, typeof(PushNotificationStrategy) }
        };
    }

    public NotificationStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public INotificationStrategy GetStrategy(NotificationType type)
    {
        if (!_strategyMap.TryGetValue(type, out var strategyType))
        {
            throw new ArgumentException($"No strategy found for notification type: {type}", nameof(type));
        }

        // Resolve the strategy from the service provider
        var strategy = _serviceProvider.GetService(strategyType) as INotificationStrategy;
        if (strategy == null)
        {
            throw new InvalidOperationException(
                $"Failed to resolve strategy of type {strategyType.Name}. Ensure it is registered in the service collection.");
        }

        return strategy;
    }

    /// <summary>
    /// Gets all supported notification types.
    /// </summary>
    /// <returns>An enumerable of supported notification types.</returns>
    public static IEnumerable<NotificationType> GetSupportedTypes() => _strategyMap.Keys;
}
