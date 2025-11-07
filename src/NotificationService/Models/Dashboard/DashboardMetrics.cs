using Core.Enums;
using MessagePack;

namespace NotificationService.Models.Dashboard;

/// <summary>
/// Real-time dashboard metrics
/// </summary>
[MessagePackObject]
public class DashboardMetrics
{
    /// <summary>
    /// Timestamp of the metrics
    /// </summary>
    [Key(0)]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Total notifications sent in the last minute
    /// </summary>
    [Key(1)]
    public int NotificationsPerMinute { get; set; }

    /// <summary>
    /// Success rate percentage (0-100)
    /// </summary>
    [Key(2)]
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    [Key(3)]
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Current active connections
    /// </summary>
    [Key(4)]
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Current active strategies count
    /// </summary>
    [Key(5)]
    public int ActiveStrategies { get; set; }

    /// <summary>
    /// Memory usage in MB
    /// </summary>
    [Key(6)]
    public double MemoryUsageMB { get; set; }

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    [Key(7)]
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Strategy-specific metrics
    /// </summary>
    [Key(8)]
    public Dictionary<NotificationType, StrategyMetrics> StrategyMetrics { get; set; } = new();

    /// <summary>
    /// Recent errors (last 10)
    /// </summary>
    [Key(9)]
    public List<string> RecentErrors { get; set; } = new();
}

/// <summary>
/// Strategy-specific performance metrics
/// </summary>
[MessagePackObject]
public class StrategyMetrics
{
    /// <summary>
    /// Number of notifications sent by this strategy
    /// </summary>
    [Key(0)]
    public int TotalSent { get; set; }

    /// <summary>
    /// Number of successful notifications
    /// </summary>
    [Key(1)]
    public int TotalSuccessful { get; set; }

    /// <summary>
    /// Number of failed notifications
    /// </summary>
    [Key(2)]
    public int TotalFailed { get; set; }

    /// <summary>
    /// Average response time for this strategy
    /// </summary>
    [Key(3)]
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Success rate for this strategy (0-100)
    /// </summary>
    [Key(4)]
    public double SuccessRate => TotalSent > 0 ? (double)TotalSuccessful / TotalSent * 100 : 0;

    /// <summary>
    /// Is this strategy currently active
    /// </summary>
    [Key(5)]
    public bool IsActive { get; set; }
}

/// <summary>
/// Real-time update message
/// </summary>
[MessagePackObject]
public class RealTimeUpdate
{
    /// <summary>
    /// Type of update
    /// </summary>
    [Key(0)]
    public UpdateType Type { get; set; }

    /// <summary>
    /// Current metrics data
    /// </summary>
    [Key(1)]
    public DashboardMetrics? Metrics { get; set; }

    /// <summary>
    /// Notification event data
    /// </summary>
    [Key(2)]
    public NotificationEvent? NotificationEvent { get; set; }

    /// <summary>
    /// System health event data
    /// </summary>
    [Key(3)]
    public HealthEvent? HealthEvent { get; set; }
}

/// <summary>
/// Type of real-time update
/// </summary>
public enum UpdateType
{
    Metrics,
    NotificationEvent,
    HealthEvent,
    ConnectionUpdate
}

/// <summary>
/// Notification event data
/// </summary>
[MessagePackObject]
public class NotificationEvent
{
    /// <summary>
    /// Event timestamp
    /// </summary>
    [Key(0)]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Notification type
    /// </summary>
    [Key(1)]
    public NotificationType Type { get; set; }

    /// <summary>
    /// Was the notification successful
    /// </summary>
    [Key(2)]
    public bool Success { get; set; }

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    [Key(3)]
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    [Key(4)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// User ID who initiated the notification
    /// </summary>
    [Key(5)]
    public string? UserId { get; set; }
}

/// <summary>
/// System health event data
/// </summary>
[MessagePackObject]
public class HealthEvent
{
    /// <summary>
    /// Event timestamp
    /// </summary>
    [Key(0)]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Health status
    /// </summary>
    [Key(1)]
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Health check name
    /// </summary>
    [Key(2)]
    public string CheckName { get; set; } = string.Empty;

    /// <summary>
    /// Health check description
    /// </summary>
    [Key(3)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional health data
    /// </summary>
    [Key(4)]
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// Historical metrics data point
/// </summary>
[MessagePackObject]
public class HistoricalMetrics
{
    /// <summary>
    /// Timestamp of the data point
    /// </summary>
    [Key(0)]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Notifications sent in this time period
    /// </summary>
    [Key(1)]
    public int NotificationsSent { get; set; }

    /// <summary>
    /// Success rate for this time period
    /// </summary>
    [Key(2)]
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average response time
    /// </summary>
    [Key(3)]
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Peak concurrent connections
    /// </summary>
    [Key(4)]
    public int PeakConnections { get; set; }
}

/// <summary>
/// Dashboard connection information
/// </summary>
[MessagePackObject]
public class ConnectionInfo
{
    /// <summary>
    /// Connection ID
    /// </summary>
    [Key(0)]
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// User ID (if authenticated)
    /// </summary>
    [Key(1)]
    public string? UserId { get; set; }

    /// <summary>
    /// Connection start time
    /// </summary>
    [Key(2)]
    public DateTimeOffset ConnectedAt { get; set; }

    /// <summary>
    /// Client type (dashboard, api, etc.)
    /// </summary>
    [Key(3)]
    public string ClientType { get; set; } = string.Empty;
}


