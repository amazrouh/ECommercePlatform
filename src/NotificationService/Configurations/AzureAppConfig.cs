namespace NotificationService.Configurations;

/// <summary>
/// Configuration settings for Azure App Configuration.
/// </summary>
public class AzureAppConfig
{
    /// <summary>
    /// Gets or sets the connection string for Azure App Configuration.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether Azure App Configuration is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the refresh interval for configuration updates.
    /// </summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the prefix for feature flags in Azure App Configuration.
    /// </summary>
    public string FeatureFlagPrefix { get; set; } = "NotificationService:FeatureManagement:";

    /// <summary>
    /// Gets or sets the list of key-value filters to use.
    /// </summary>
    public List<string> KeyValueFilters { get; set; } = new() { "NotificationService:*" };

    /// <summary>
    /// Gets or sets the sentinel key for configuration refresh.
    /// </summary>
    public string SentinelKey { get; set; } = "NotificationService:Config:Sentinel";

    /// <summary>
    /// Gets or sets whether to use Azure Key Vault references.
    /// </summary>
    public bool UseKeyVault { get; set; } = true;
}
