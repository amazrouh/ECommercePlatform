using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Hubs;
using NotificationService.Models.Dashboard;
using NotificationService.Services;

namespace NotificationService.Controllers;

/// <summary>
/// Controller for dashboard data and historical metrics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class DashboardController : ControllerBase
{
    private readonly DashboardMetricsService _metricsService;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        DashboardMetricsService metricsService,
        IAuditLogger auditLogger,
        ILogger<DashboardController> logger)
    {
        _metricsService = metricsService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    /// <summary>
    /// Get current real-time metrics
    /// </summary>
    /// <returns>Current dashboard metrics</returns>
    /// <response code="200">Returns current metrics</response>
    [HttpGet("metrics/current")]
    [ProducesResponseType(typeof(DashboardMetrics), 200)]
    public ActionResult<DashboardMetrics> GetCurrentMetrics()
    {
        var metrics = _metricsService.GetCurrentMetrics();

        _logger.LogInformation("Dashboard metrics requested by user {UserId}",
            User.Identity?.Name);

        return Ok(metrics);
    }

    /// <summary>
    /// Get strategy-specific metrics
    /// </summary>
    /// <param name="type">Notification type to get metrics for</param>
    /// <returns>Strategy metrics for the specified type</returns>
    /// <response code="200">Returns strategy metrics</response>
    /// <response code="404">If strategy type not found</response>
    [HttpGet("metrics/strategy/{type}")]
    [ProducesResponseType(typeof(StrategyMetrics), 200)]
    [ProducesResponseType(404)]
    public ActionResult<StrategyMetrics> GetStrategyMetrics(Core.Enums.NotificationType type)
    {
        var metrics = _metricsService.GetStrategyMetrics(type);

        if (metrics == null)
        {
            return NotFound($"Strategy metrics not found for type: {type}");
        }

        return Ok(metrics);
    }

    /// <summary>
    /// Get all strategy metrics
    /// </summary>
    /// <returns>Dictionary of all strategy metrics</returns>
    /// <response code="200">Returns all strategy metrics</response>
    [HttpGet("metrics/strategies")]
    [ProducesResponseType(typeof(Dictionary<Core.Enums.NotificationType, StrategyMetrics>), 200)]
    public ActionResult<Dictionary<Core.Enums.NotificationType, StrategyMetrics>> GetAllStrategyMetrics()
    {
        var currentMetrics = _metricsService.GetCurrentMetrics();
        return Ok(currentMetrics.StrategyMetrics);
    }

    /// <summary>
    /// Get active dashboard connections
    /// </summary>
    /// <returns>List of active dashboard connections</returns>
    /// <response code="200">Returns active connections</response>
    [HttpGet("connections")]
    [ProducesResponseType(typeof(IEnumerable<Models.Dashboard.ConnectionInfo>), 200)]
    public ActionResult<IEnumerable<Models.Dashboard.ConnectionInfo>> GetActiveConnections()
    {
        var connections = NotificationHub.GetActiveConnections();
        return Ok(connections);
    }

    /// <summary>
    /// Get connection count
    /// </summary>
    /// <returns>Current connection count</returns>
    /// <response code="200">Returns connection count</response>
    [HttpGet("connections/count")]
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult<object> GetConnectionCount()
    {
        var count = NotificationHub.GetConnectionCount();
        return Ok(new { activeConnections = count });
    }

    /// <summary>
    /// Get system health overview
    /// </summary>
    /// <returns>System health information</returns>
    /// <response code="200">Returns system health</response>
    [HttpGet("health")]
    [AllowAnonymous] // Allow health checks without authentication
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult<object> GetSystemHealth()
    {
        var metrics = _metricsService.GetCurrentMetrics();

        var health = new
        {
            status = metrics.SuccessRate >= 95 ? "healthy" : metrics.SuccessRate >= 80 ? "degraded" : "unhealthy",
            timestamp = metrics.Timestamp,
            metrics = new
            {
                notificationsPerMinute = metrics.NotificationsPerMinute,
                successRate = metrics.SuccessRate,
                averageResponseTimeMs = metrics.AverageResponseTimeMs,
                activeConnections = metrics.ActiveConnections,
                activeStrategies = metrics.ActiveStrategies,
                memoryUsageMB = metrics.MemoryUsageMB,
                cpuUsagePercent = metrics.CpuUsagePercent
            },
            strategies = metrics.StrategyMetrics.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    totalSent = kvp.Value.TotalSent,
                    successRate = kvp.Value.SuccessRate,
                    averageResponseTimeMs = kvp.Value.AverageResponseTimeMs,
                    isActive = kvp.Value.IsActive
                }),
            recentErrors = metrics.RecentErrors
        };

        return Ok(health);
    }

    /// <summary>
    /// Reset metrics counters (admin only)
    /// </summary>
    /// <returns>Success message</returns>
    /// <response code="200">Metrics reset successfully</response>
    /// <response code="403">Forbidden if not admin</response>
    [HttpPost("metrics/reset")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> ResetMetrics()
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        _metricsService.ResetMetrics();

        await _auditLogger.LogSecurityEventAsync("MetricsReset",
            "Dashboard metrics were manually reset",
            User.Identity?.Name);

        _logger.LogWarning("Metrics reset by user {UserId}", User.Identity?.Name);

        return Ok(new { message = "Metrics reset successfully" });
    }

    /// <summary>
    /// Get historical metrics for a time range
    /// </summary>
    /// <param name="startTime">Start time for historical data</param>
    /// <param name="endTime">End time for historical data</param>
    /// <param name="intervalMinutes">Interval in minutes for data aggregation</param>
    /// <returns>Historical metrics data</returns>
    /// <response code="200">Returns historical metrics</response>
    /// <response code="400">If time range is invalid</response>
    [HttpGet("metrics/history")]
    [ProducesResponseType(typeof(IEnumerable<HistoricalMetrics>), 200)]
    [ProducesResponseType(400)]
    public ActionResult<IEnumerable<HistoricalMetrics>> GetHistoricalMetrics(
        [FromQuery] DateTimeOffset startTime,
        [FromQuery] DateTimeOffset endTime,
        [FromQuery] int intervalMinutes = 5)
    {
        if (startTime >= endTime)
        {
            return BadRequest("Start time must be before end time");
        }

        if (intervalMinutes < 1 || intervalMinutes > 60)
        {
            return BadRequest("Interval must be between 1 and 60 minutes");
        }

        // For now, return current metrics as a single data point
        // In a real implementation, this would query historical data from a database
        var currentMetrics = _metricsService.GetCurrentMetrics();

        var historicalData = new List<HistoricalMetrics>
        {
            new HistoricalMetrics
            {
                Timestamp = currentMetrics.Timestamp,
                NotificationsSent = currentMetrics.NotificationsPerMinute,
                SuccessRate = currentMetrics.SuccessRate,
                AverageResponseTimeMs = currentMetrics.AverageResponseTimeMs,
                PeakConnections = currentMetrics.ActiveConnections
            }
        };

        return Ok(historicalData);
    }

    /// <summary>
    /// Get dashboard summary statistics
    /// </summary>
    /// <returns>Dashboard summary</returns>
    /// <response code="200">Returns dashboard summary</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult<object> GetDashboardSummary()
    {
        var metrics = _metricsService.GetCurrentMetrics();

        var summary = new
        {
            totalNotifications = metrics.StrategyMetrics.Sum(s => s.Value.TotalSent),
            totalSuccessful = metrics.StrategyMetrics.Sum(s => s.Value.TotalSuccessful),
            totalFailed = metrics.StrategyMetrics.Sum(s => s.Value.TotalFailed),
            overallSuccessRate = metrics.SuccessRate,
            activeStrategies = metrics.ActiveStrategies,
            systemLoad = new
            {
                cpuPercent = metrics.CpuUsagePercent,
                memoryMB = metrics.MemoryUsageMB
            },
            connections = new
            {
                active = metrics.ActiveConnections,
                peak = metrics.ActiveConnections // In real implementation, track peak
            },
            topPerformingStrategy = metrics.StrategyMetrics
                .OrderByDescending(s => s.Value.SuccessRate)
                .FirstOrDefault().Key,
            alerts = metrics.RecentErrors.Count > 0 ? "Recent errors detected" : "System healthy"
        };

        return Ok(summary);
    }
}
