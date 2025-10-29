using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace NotificationService.Decorators;

/// <summary>
/// Decorator that adds circuit breaker capability to notification operations.
/// </summary>
public class CircuitBreakerNotificationDecorator : INotificationService
{
    private readonly INotificationService _inner;
    private readonly ILogger<CircuitBreakerNotificationDecorator> _logger;
    private readonly AsyncCircuitBreakerPolicy<NotificationResult> _circuitBreaker;
    private readonly AsyncCircuitBreakerPolicy<IDictionary<NotificationType, NotificationResult>> _batchCircuitBreaker;

    public CircuitBreakerNotificationDecorator(
        INotificationService inner,
        ILogger<CircuitBreakerNotificationDecorator> logger)
    {
        _inner = inner;
        _logger = logger;

        _circuitBreaker = Policy<NotificationResult>
            .Handle<Exception>()
            .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.5,
                samplingDuration: TimeSpan.FromSeconds(30),
                minimumThroughput: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                (result, duration) => OnBreak(result.Exception ?? new Exception("Unknown error"), duration),
                () => OnReset(),
                () => OnHalfOpen());

        _batchCircuitBreaker = Policy<IDictionary<NotificationType, NotificationResult>>
            .Handle<Exception>()
            .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.5,
                samplingDuration: TimeSpan.FromSeconds(30),
                minimumThroughput: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                (result, duration) => OnBatchBreak(result.Exception ?? new Exception("Unknown error"), duration),
                () => OnBatchReset(),
                () => OnBatchHalfOpen());
    }

    public async Task<NotificationResult> SendAsync(
        NotificationType type,
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async (ct) =>
                await _inner.SendAsync(type, message, ct), cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(
                ex,
                "Circuit breaker is open. Cannot send {Type} notification to {Recipient}",
                type, message.To);

            return NotificationResult.Failed("Service is temporarily unavailable due to too many failures.");
        }
    }

    public async Task<IDictionary<NotificationType, NotificationResult>> SendBatchAsync(
        IDictionary<NotificationType, NotificationMessage> notifications,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _batchCircuitBreaker.ExecuteAsync(async (ct) =>
                await _inner.SendBatchAsync(notifications, ct), cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(
                ex,
                "Circuit breaker is open. Cannot send batch of {Count} notifications",
                notifications.Count);

            return notifications.ToDictionary(
                n => n.Key,
                _ => NotificationResult.Failed("Service is temporarily unavailable due to too many failures."));
        }
    }

    public Task<IEnumerable<NotificationType>> GetSupportedTypes()
        => _inner.GetSupportedTypes();

    private void OnBreak(Exception ex, TimeSpan duration)
    {
        _logger.LogError(
            ex,
            "Circuit breaker tripped. Service will be unavailable for {Duration}s",
            duration.TotalSeconds);
    }

    private void OnReset()
    {
        _logger.LogInformation("Circuit breaker reset. Service is available again.");
    }

    private void OnHalfOpen()
    {
        _logger.LogInformation("Circuit breaker is half-open. Testing service availability.");
    }

    private void OnBatchBreak(Exception ex, TimeSpan duration)
    {
        _logger.LogError(
            ex,
            "Batch circuit breaker tripped. Service will be unavailable for {Duration}s",
            duration.TotalSeconds);
    }

    private void OnBatchReset()
    {
        _logger.LogInformation("Batch circuit breaker reset. Service is available again.");
    }

    private void OnBatchHalfOpen()
    {
        _logger.LogInformation("Batch circuit breaker is half-open. Testing service availability.");
    }
}
