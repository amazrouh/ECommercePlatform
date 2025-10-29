using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using NotificationService.Caching;

namespace NotificationService.Decorators;

/// <summary>
/// Decorator that adds caching capabilities to the notification service.
/// </summary>
public class CachingNotificationDecorator : INotificationService
{
    private readonly INotificationService _inner;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachingNotificationDecorator> _logger;
    private const string SupportedTypesKey = "notification:supported-types";
    private static readonly TimeSpan SupportedTypesCacheDuration = TimeSpan.FromHours(24);

    public CachingNotificationDecorator(
        INotificationService inner,
        ICacheService cacheService,
        ILogger<CachingNotificationDecorator> logger)
    {
        _inner = inner;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<NotificationResult> SendAsync(
        NotificationType type,
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        // Don't cache individual send operations as they are not idempotent
        return await _inner.SendAsync(type, message, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IDictionary<NotificationType, NotificationResult>> SendBatchAsync(
        IDictionary<NotificationType, NotificationMessage> notifications,
        CancellationToken cancellationToken = default)
    {
        // Don't cache batch send operations as they are not idempotent
        return await _inner.SendBatchAsync(notifications, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<NotificationType>> GetSupportedTypes()
    {
        try
        {
            // Try to get from cache
            var cachedTypes = await _cacheService.GetAsync<NotificationType[]>(SupportedTypesKey)
                .ConfigureAwait(false);

            if (cachedTypes != null)
            {
                _logger.LogDebug("Retrieved supported types from cache");
                return cachedTypes;
            }

            // Get from inner service and cache
            var types = (await _inner.GetSupportedTypes().ConfigureAwait(false)).ToArray();
            await _cacheService.SetAsync(
                SupportedTypesKey,
                types,
                DateTimeOffset.UtcNow.Add(SupportedTypesCacheDuration))
                .ConfigureAwait(false);

            _logger.LogDebug("Cached supported types");
            return types;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported types from cache");
            return await _inner.GetSupportedTypes().ConfigureAwait(false);
        }
    }
}
