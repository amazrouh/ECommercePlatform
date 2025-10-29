namespace NotificationService.Configurations;

/// <summary>
/// Configuration settings for push notifications.
/// </summary>
public class PushConfig
{
    /// <summary>
    /// Gets or sets the Firebase Cloud Messaging server key.
    /// </summary>
    public string FcmServerKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Apple Push Notification Service key ID.
    /// </summary>
    public string ApnsKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Apple Push Notification Service team ID.
    /// </summary>
    public string ApnsTeamId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Apple Push Notification Service bundle ID.
    /// </summary>
    public string ApnsBundleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the Apple Push Notification Service private key.
    /// </summary>
    public string ApnsPrivateKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default time-to-live for push notifications (in seconds).
    /// </summary>
    public int DefaultTtl { get; set; } = 3600;

    /// <summary>
    /// Gets or sets the maximum payload size in bytes.
    /// </summary>
    public int MaxPayloadSize { get; set; } = 4096;
}
