namespace NotificationService.Configurations;

/// <summary>
/// Configuration for security settings
/// </summary>
public class SecurityConfig
{
    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public RateLimitConfig RateLimit { get; set; } = new();

    /// <summary>
    /// Security headers configuration
    /// </summary>
    public SecurityHeadersConfig SecurityHeaders { get; set; } = new();
}

/// <summary>
/// Rate limiting configuration
/// </summary>
public class RateLimitConfig
{
    /// <summary>
    /// Maximum number of requests per time window
    /// </summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>
    /// Time window in seconds for rate limiting
    /// </summary>
    public int WindowSeconds { get; set; } = 10;
}

/// <summary>
/// Security headers configuration
/// </summary>
public class SecurityHeadersConfig
{
    /// <summary>
    /// X-Content-Type-Options header value
    /// </summary>
    public string ContentTypeOptions { get; set; } = "nosniff";

    /// <summary>
    /// X-Frame-Options header value
    /// </summary>
    public string FrameOptions { get; set; } = "DENY";

    /// <summary>
    /// Referrer-Policy header value
    /// </summary>
    public string ReferrerPolicy { get; set; } = "no-referrer";

    /// <summary>
    /// Content-Security-Policy header value
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; font-src 'self'; connect-src 'self'; media-src 'none'; object-src 'none'; child-src 'none'; worker-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self';";
}
