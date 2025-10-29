using System.Collections.Immutable;

namespace Core.Models;

/// <summary>
/// Represents a notification message to be sent.
/// </summary>
public sealed record NotificationMessage
{
    /// <summary>
    /// Gets the recipient of the notification.
    /// </summary>
    public string To { get; }

    /// <summary>
    /// Gets the subject of the notification.
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Gets the body content of the notification.
    /// </summary>
    public string Body { get; }

    /// <summary>
    /// Gets additional metadata associated with the notification.
    /// </summary>
    public IImmutableDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the NotificationMessage class.
    /// </summary>
    /// <param name="to">The recipient of the notification.</param>
    /// <param name="subject">The subject of the notification.</param>
    /// <param name="body">The body content of the notification.</param>
    /// <param name="metadata">Additional metadata for the notification (optional).</param>
    /// <exception cref="ArgumentException">Thrown when required parameters are null or empty.</exception>
    public NotificationMessage(
        string to,
        string subject,
        string body,
        IDictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("Recipient cannot be empty.", nameof(to));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty.", nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body cannot be empty.", nameof(body));

        To = to;
        Subject = subject;
        Body = body;
        Metadata = metadata?.ToImmutableDictionary() ?? ImmutableDictionary<string, object>.Empty;
    }

    /// <summary>
    /// Creates a new NotificationMessage with updated metadata.
    /// </summary>
    /// <param name="metadata">The new metadata dictionary.</param>
    /// <returns>A new NotificationMessage instance with updated metadata.</returns>
    public NotificationMessage WithMetadata(IDictionary<string, object> metadata)
        => new NotificationMessage(To, Subject, Body, metadata);
}
