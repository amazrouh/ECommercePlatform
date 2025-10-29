namespace Core.Models;

/// <summary>
/// Represents the result of a notification delivery attempt.
/// </summary>
public sealed record NotificationResult
{
    /// <summary>
    /// Gets whether the notification was successfully sent.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the unique message identifier from the notification provider.
    /// </summary>
    public string? MessageId { get; }

    /// <summary>
    /// Gets the error message if the notification failed.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the timestamp when the notification was sent.
    /// </summary>
    public DateTimeOffset SentAt { get; }

    private NotificationResult(bool success, string? messageId, string? error)
    {
        Success = success;
        MessageId = messageId;
        Error = error;
        SentAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a successful notification result.
    /// </summary>
    /// <param name="messageId">The unique message identifier from the provider.</param>
    /// <returns>A successful NotificationResult instance.</returns>
    public static NotificationResult Succeeded(string messageId)
        => new(true, messageId, null);

    /// <summary>
    /// Creates a failed notification result.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A failed NotificationResult instance.</returns>
    public static NotificationResult Failed(string error)
        => new(false, null, error);
}
