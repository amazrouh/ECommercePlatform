using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace NotificationService.Decorators;

/// <summary>
/// Decorator that adds retry capability to notification operations.
/// </summary>
public class RetryNotificationDecorator : INotificationService
{
    private readonly INotificationService _inner;
    private readonly ILogger<RetryNotificationDecorator> _logger;
    private readonly AsyncRetryPolicy<NotificationResult> _retryPolicy;
    private readonly AsyncRetryPolicy<IDictionary<NotificationType, NotificationResult>> _batchRetryPolicy;

    public RetryNotificationDecorator(
        INotificationService inner,
        ILogger<RetryNotificationDecorator> logger)
    {
        _inner = inner;
        _logger = logger;

        _retryPolicy = Policy<NotificationResult>
            .Handle<Exception>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (result, delay, attempt, ctx) => OnRetry(result.Exception ?? new Exception("Unknown error"), delay, attempt, ctx));

        _batchRetryPolicy = Policy<IDictionary<NotificationType, NotificationResult>>
            .Handle<Exception>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (result, delay, attempt, ctx) => OnBatchRetry(result.Exception ?? new Exception("Unknown error"), delay, attempt, ctx));
    }

    public Task<NotificationResult> SendAsync(
        NotificationType type,
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        return _retryPolicy.ExecuteAsync(async (ct) =>
            await _inner.SendAsync(type, message, ct), cancellationToken);
    }

    public Task<IDictionary<NotificationType, NotificationResult>> SendBatchAsync(
        IDictionary<NotificationType, NotificationMessage> notifications,
        CancellationToken cancellationToken = default)
    {
        return _batchRetryPolicy.ExecuteAsync(async (ct) =>
            await _inner.SendBatchAsync(notifications, ct), cancellationToken);
    }

    public Task<IEnumerable<NotificationType>> GetSupportedTypes()
        => _inner.GetSupportedTypes();

    private void OnRetry(Exception ex, TimeSpan delay, int attempt, Context context)
    {
        _logger.LogWarning(
            ex,
            "Retry attempt {Attempt} after {Delay}ms delay due to error: {Error}",
            attempt, delay.TotalMilliseconds, ex.Message);
    }

    private void OnBatchRetry(Exception ex, TimeSpan delay, int attempt, Context context)
    {
        _logger.LogWarning(
            ex,
            "Batch retry attempt {Attempt} after {Delay}ms delay due to error: {Error}",
            attempt, delay.TotalMilliseconds, ex.Message);
    }
}
