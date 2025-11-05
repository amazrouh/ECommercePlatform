using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace NotificationService.Services;

/// <summary>
/// Implementation of audit logging for security and notification events
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task LogNotificationSentAsync(NotificationMessage message, NotificationResult result, string? userId = null)
    {
        var auditEntry = new
        {
            EventType = "NotificationSent",
            Timestamp = DateTimeOffset.UtcNow,
            UserId = userId,
            NotificationType = message.Subject,
            To = message.To,
            MessageId = result.MessageId,
            SentAt = result.SentAt
        };

        _logger.LogInformation("AUDIT: Notification sent - {@AuditEntry}", auditEntry);
        await Task.CompletedTask; // For async compatibility
    }

    /// <inheritdoc/>
    public async Task LogNotificationFailedAsync(NotificationMessage message, string error, string? userId = null)
    {
        var auditEntry = new
        {
            EventType = "NotificationFailed",
            Timestamp = DateTimeOffset.UtcNow,
            UserId = userId,
            NotificationType = message.Subject,
            To = message.To,
            Error = error
        };

        _logger.LogWarning("AUDIT: Notification failed - {@AuditEntry}", auditEntry);
        await Task.CompletedTask; // For async compatibility
    }

    /// <inheritdoc/>
    public async Task LogAuthenticationFailureAsync(string username, string ipAddress, string reason)
    {
        var auditEntry = new
        {
            EventType = "AuthenticationFailure",
            Timestamp = DateTimeOffset.UtcNow,
            Username = username,
            IpAddress = ipAddress,
            Reason = reason
        };

        _logger.LogWarning("AUDIT: Authentication failure - {@AuditEntry}", auditEntry);
        await Task.CompletedTask; // For async compatibility
    }

    /// <inheritdoc/>
    public async Task LogAuthenticationSuccessAsync(string username, string ipAddress)
    {
        var auditEntry = new
        {
            EventType = "AuthenticationSuccess",
            Timestamp = DateTimeOffset.UtcNow,
            Username = username,
            IpAddress = ipAddress
        };

        _logger.LogInformation("AUDIT: Authentication success - {@AuditEntry}", auditEntry);
        await Task.CompletedTask; // For async compatibility
    }

    /// <inheritdoc/>
    public async Task LogAuthorizationFailureAsync(string userId, string resource, string action, string ipAddress)
    {
        var auditEntry = new
        {
            EventType = "AuthorizationFailure",
            Timestamp = DateTimeOffset.UtcNow,
            UserId = userId,
            Resource = resource,
            Action = action,
            IpAddress = ipAddress
        };

        _logger.LogWarning("AUDIT: Authorization failure - {@AuditEntry}", auditEntry);
        await Task.CompletedTask; // For async compatibility
    }

    /// <inheritdoc/>
    public async Task LogSecurityEventAsync(string eventType, string description, string? userId = null, string? ipAddress = null, IDictionary<string, object>? additionalData = null)
    {
        var auditEntry = new
        {
            EventType = eventType,
            Timestamp = DateTimeOffset.UtcNow,
            UserId = userId,
            IpAddress = ipAddress,
            Description = description,
            AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
        };

        _logger.LogInformation("AUDIT: Security event - {@AuditEntry}", auditEntry);
        await Task.CompletedTask; // For async compatibility
    }
}
