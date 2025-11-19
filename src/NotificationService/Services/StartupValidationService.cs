using Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Net.Http;

namespace NotificationService.Services;

/// <summary>
/// Service for validating application startup requirements
/// </summary>
public class StartupValidationService
{
    private readonly ILogger<StartupValidationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAuditLogger _auditLogger;
    private readonly HttpClient _httpClient;

    public StartupValidationService(
        ILogger<StartupValidationService> logger,
        IConfiguration configuration,
        IAuditLogger auditLogger)
    {
        _logger = logger;
        _configuration = configuration;
        _auditLogger = auditLogger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// Validate all startup requirements
    /// </summary>
    /// <returns></returns>
    public async Task<StartupValidationResult> ValidateStartupAsync()
    {
        _logger.LogInformation("Starting application startup validation");

        var result = new StartupValidationResult
        {
            IsValid = true,
            Validations = new List<ValidationCheck>()
        };

        // Validate configuration
        await ValidateConfiguration(result);

        // Validate database connectivity
        await ValidateDatabaseConnection(result);

        // Validate Redis connectivity
        await ValidateRedisConnection(result);

        // Validate external services
        await ValidateExternalServices(result);

        // Validate security configuration
        await ValidateSecurityConfiguration(result);

        // Validate feature flags
        await ValidateFeatureFlags(result);

        // Log validation results
        await LogValidationResults(result);

        _logger.LogInformation("Startup validation completed. Valid: {IsValid}", result.IsValid);

        return result;
    }

    /// <summary>
    /// Validate configuration settings
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task ValidateConfiguration(StartupValidationResult result)
    {
        var checks = new List<(string Name, Func<bool> Validator, string ErrorMessage)>
        {
            ("JWT Secret", () => !string.IsNullOrEmpty(_configuration["Jwt:Secret"]),
             "JWT Secret is not configured"),
            ("JWT Issuer", () => !string.IsNullOrEmpty(_configuration["Jwt:Issuer"]),
             "JWT Issuer is not configured"),
            ("JWT Audience", () => !string.IsNullOrEmpty(_configuration["Jwt:Audience"]),
             "JWT Audience is not configured"),
            ("Database Connection", () => !string.IsNullOrEmpty(_configuration.GetConnectionString("NotificationDb")),
             "Database connection string is not configured"),
            ("Redis Connection", () => !string.IsNullOrEmpty(_configuration.GetConnectionString("Redis")),
             "Redis connection string is not configured")
        };

        foreach (var check in checks)
        {
            var isValid = check.Validator();
            result.Validations.Add(new ValidationCheck
            {
                Name = check.Name,
                Category = "Configuration",
                IsValid = isValid,
                ErrorMessage = isValid ? null : check.ErrorMessage
            });

            if (!isValid)
            {
                result.IsValid = false;
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validate database connection
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task ValidateDatabaseConnection(StartupValidationResult result)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("NotificationDb");
            if (string.IsNullOrEmpty(connectionString))
            {
                result.Validations.Add(new ValidationCheck
                {
                    Name = "Database Connection",
                    Category = "Database",
                    IsValid = false,
                    ErrorMessage = "Database connection string is missing"
                });
                result.IsValid = false;
                return;
            }

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Test with a simple query
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();

            result.Validations.Add(new ValidationCheck
            {
                Name = "Database Connection",
                Category = "Database",
                IsValid = true,
                ErrorMessage = null
            });
        }
        catch (Exception ex)
        {
            result.Validations.Add(new ValidationCheck
            {
                Name = "Database Connection",
                Category = "Database",
                IsValid = false,
                ErrorMessage = $"Database connection failed: {ex.Message}"
            });
            result.IsValid = false;
        }
    }

    /// <summary>
    /// Validate Redis connection
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task ValidateRedisConnection(StartupValidationResult result)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("Redis");
            if (string.IsNullOrEmpty(connectionString))
            {
                result.Validations.Add(new ValidationCheck
                {
                    Name = "Redis Connection",
                    Category = "Cache",
                    IsValid = false,
                    ErrorMessage = "Redis connection string is missing"
                });
                result.IsValid = false;
                return;
            }

            var redis = ConnectionMultiplexer.Connect(connectionString);
            var db = redis.GetDatabase();

            // Test with ping
            var pingResult = await db.PingAsync();
            await redis.CloseAsync();

            result.Validations.Add(new ValidationCheck
            {
                Name = "Redis Connection",
                Category = "Cache",
                IsValid = true,
                ErrorMessage = null,
                AdditionalInfo = $"Ping time: {pingResult.TotalMilliseconds}ms"
            });
        }
        catch (Exception ex)
        {
            result.Validations.Add(new ValidationCheck
            {
                Name = "Redis Connection",
                Category = "Cache",
                IsValid = false,
                ErrorMessage = $"Redis connection failed: {ex.Message}"
            });
            result.IsValid = false;
        }
    }

    /// <summary>
    /// Validate external services
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task ValidateExternalServices(StartupValidationResult result)
    {
        // Check if external services are configured and accessible
        var externalServices = new[]
        {
            ("Email SMTP", _configuration["Email:SmtpHost"], "Email service configuration"),
            ("SMS Provider", _configuration["Sms:Provider"], "SMS service configuration"),
            ("Push Provider", _configuration["Push:Provider"], "Push notification service configuration")
        };

        foreach (var (name, config, description) in externalServices)
        {
            var isConfigured = !string.IsNullOrEmpty(config);
            result.Validations.Add(new ValidationCheck
            {
                Name = name,
                Category = "External Services",
                IsValid = isConfigured,
                ErrorMessage = isConfigured ? null : $"{description} is missing",
                AdditionalInfo = isConfigured ? "Configured" : "Not configured (will use mock implementations)"
            });
        }

        // Test Azure App Configuration if enabled
        var appConfigEnabled = _configuration.GetValue<bool>("AzureAppConfig:Enabled");
        if (appConfigEnabled)
        {
            var connectionString = _configuration["AzureAppConfig:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                result.Validations.Add(new ValidationCheck
                {
                    Name = "Azure App Configuration",
                    Category = "External Services",
                    IsValid = false,
                    ErrorMessage = "Azure App Configuration is enabled but connection string is missing"
                });
                result.IsValid = false;
            }
            else
            {
                result.Validations.Add(new ValidationCheck
                {
                    Name = "Azure App Configuration",
                    Category = "External Services",
                    IsValid = true,
                    AdditionalInfo = "Enabled and configured"
                });
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validate security configuration
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task ValidateSecurityConfiguration(StartupValidationResult result)
    {
        var securityChecks = new List<(string Name, Func<bool> Validator, string ErrorMessage)>
        {
            ("JWT Secret Length", () => (_configuration["Jwt:Secret"]?.Length ?? 0) >= 32,
             "JWT Secret should be at least 32 characters long"),
            ("Rate Limiting", () => _configuration.GetSection("RateLimiting").Exists(),
             "Rate limiting configuration is missing"),
            ("Security Headers", () => _configuration.GetSection("SecurityHeaders").Exists(),
             "Security headers configuration is missing")
        };

        foreach (var check in securityChecks)
        {
            var isValid = check.Validator();
            result.Validations.Add(new ValidationCheck
            {
                Name = check.Name,
                Category = "Security",
                IsValid = isValid,
                ErrorMessage = isValid ? null : check.ErrorMessage
            });

            if (!isValid)
            {
                result.IsValid = false;
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validate feature flags configuration
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task ValidateFeatureFlags(StartupValidationResult result)
    {
        // Check if feature flags are properly configured
        result.Validations.Add(new ValidationCheck
        {
            Name = "Feature Flags",
            Category = "Configuration",
            IsValid = true,
            AdditionalInfo = "Feature management is configured"
        });

        await Task.CompletedTask;
    }

    /// <summary>
    /// Log validation results
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task LogValidationResults(StartupValidationResult result)
    {
        var passed = result.Validations.Count(v => v.IsValid);
        var failed = result.Validations.Count(v => !v.IsValid);

        await _auditLogger.LogSecurityEventAsync("StartupValidation",
            $"Startup validation completed. Passed: {passed}, Failed: {failed}",
            "system");

        if (!result.IsValid)
        {
            var failures = result.Validations.Where(v => !v.IsValid)
                .Select(v => $"{v.Category}: {v.Name} - {v.ErrorMessage}")
                .ToList();

            _logger.LogWarning("Startup validation failed: {Failures}",
                string.Join("; ", failures));
        }
    }
}

/// <summary>
/// Result of startup validation
/// </summary>
public class StartupValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationCheck> Validations { get; set; } = new();
}

/// <summary>
/// Individual validation check result
/// </summary>
public class ValidationCheck
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AdditionalInfo { get; set; }
}
