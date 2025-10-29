namespace NotificationService.Configurations;

/// <summary>
/// Configuration settings for SMS notifications.
/// </summary>
public class SmsConfig
{
    /// <summary>
    /// Gets or sets the SMS API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMS API secret.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender phone number.
    /// </summary>
    public string FromNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum message length.
    /// </summary>
    public int MessageLengthLimit { get; set; }

    /// <summary>
    /// Gets or sets the maximum message length (alias for MessageLengthLimit).
    /// </summary>
    public int MaxMessageLength => MessageLengthLimit;

    /// <summary>
    /// Gets or sets whether to split long messages into multiple SMS.
    /// </summary>
    public bool SplitLongMessages { get; set; }
}
