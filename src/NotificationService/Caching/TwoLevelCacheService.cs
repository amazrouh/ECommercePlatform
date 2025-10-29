using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace NotificationService.Caching;

/// <summary>
/// Implements a two-level caching strategy using memory cache (L1) and distributed cache (L2).
/// </summary>
public class TwoLevelCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<TwoLevelCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public TwoLevelCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<TwoLevelCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Try L1 cache first
            if (_memoryCache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("Cache hit (L1) for key: {Key}", key);
                return value;
            }

            // Try L2 cache
            var json = await _distributedCache.GetStringAsync(key, cancellationToken)
                .ConfigureAwait(false);

            if (json == null)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            // Deserialize and promote to L1 cache
            value = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            if (value != null)
            {
                _logger.LogDebug("Cache hit (L2) for key: {Key}", key);
                _memoryCache.Set(key, value);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, DateTimeOffset absoluteExpiration, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var timeToLive = absoluteExpiration - DateTimeOffset.UtcNow;
            if (timeToLive <= TimeSpan.Zero)
            {
                _logger.LogWarning("Attempted to cache with expired TTL for key: {Key}", key);
                return;
            }

            // Set L1 cache
            var memoryCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration
            };
            _memoryCache.Set(key, value, memoryCacheOptions);

            // Set L2 cache
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var distributedCacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration
            };

            await _distributedCache.SetStringAsync(key, json, distributedCacheOptions, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug("Value cached for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            // Remove from both caches
            _memoryCache.Remove(key);
            await _distributedCache.RemoveAsync(key, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug("Value removed from cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from cache for key: {Key}", key);
        }
    }
}
