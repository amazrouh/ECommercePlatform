namespace NotificationService.Configurations;

/// <summary>
/// Configuration for JWT token settings
/// </summary>
public class JwtConfig
{
    /// <summary>
    /// JWT secret key used for signing tokens
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// JWT issuer (who issued the token)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// JWT audience (who the token is intended for)
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}
