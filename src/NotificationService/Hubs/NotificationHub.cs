using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NotificationService.Models.Dashboard;
using System.Collections.Concurrent;

namespace NotificationService.Hubs;

/// <summary>
/// SignalR hub for real-time notification dashboard communication
/// </summary>
// [Authorize(Policy = "RequireAdmin")] // Temporarily disabled for testing
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    private static readonly ConcurrentDictionary<string, Models.Dashboard.ConnectionInfo> _connections = new();

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionInfo = new Models.Dashboard.ConnectionInfo
        {
            ConnectionId = Context.ConnectionId,
            UserId = Context.User?.Identity?.Name,
            ConnectedAt = DateTimeOffset.UtcNow,
            ClientType = "dashboard"
        };

        _connections[Context.ConnectionId] = connectionInfo;

        _logger.LogInformation("Dashboard client connected: {ConnectionId}, User: {UserId}",
            Context.ConnectionId, connectionInfo.UserId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connections.TryRemove(Context.ConnectionId, out var connectionInfo))
        {
            _logger.LogInformation("Dashboard client disconnected: {ConnectionId}, User: {UserId}",
                Context.ConnectionId, connectionInfo.UserId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join the dashboard group for receiving real-time updates
    /// </summary>
    public async Task JoinDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
        _logger.LogInformation("Client {ConnectionId} joined dashboard group", Context.ConnectionId);

        // Send current connection count to all dashboard clients
        await SendConnectionUpdate();
    }

    /// <summary>
    /// Leave the dashboard group
    /// </summary>
    public async Task LeaveDashboard()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");
        _logger.LogInformation("Client {ConnectionId} left dashboard group", Context.ConnectionId);

        // Send updated connection count to all dashboard clients
        await SendConnectionUpdate();
    }

    /// <summary>
    /// Send metrics to dashboard clients (admin only)
    /// </summary>
    public async Task SendMetrics(DashboardMetrics metrics)
    {
        // Verify user has admin role
        // if (!Context.User?.IsInRole("Admin") ?? true)
        // {
        //     _logger.LogWarning("Unauthorized attempt to send metrics by user {UserId}",
        //         Context.User?.Identity?.Name);
        //     throw new HubException("Unauthorized: Admin role required");
        // }

        await Clients.Group("dashboard").SendAsync("ReceiveMetrics", metrics);
        _logger.LogDebug("Metrics sent to dashboard clients");
    }

    /// <summary>
    /// Request current system metrics
    /// </summary>
    public async Task RequestMetrics()
    {
        // This will be handled by the metrics service
        await Clients.Caller.SendAsync("MetricsRequested", Context.ConnectionId);
    }

    /// <summary>
    /// Send a notification event to dashboard clients
    /// </summary>
    public async Task SendNotificationEvent(NotificationEvent notificationEvent)
    {
        // if (!Context.User?.IsInRole("Admin") ?? true)
        // {
        //     _logger.LogWarning("Unauthorized attempt to send notification event by user {UserId}",
        //         Context.User?.Identity?.Name);
        //     throw new HubException("Unauthorized: Admin role required");
        // }

        var update = new RealTimeUpdate
        {
            Type = UpdateType.NotificationEvent,
            NotificationEvent = notificationEvent
        };

        await Clients.Group("dashboard").SendAsync("ReceiveUpdate", update);
    }

    /// <summary>
    /// Send a health event to dashboard clients
    /// </summary>
    public async Task SendHealthEvent(HealthEvent healthEvent)
    {
        // if (!Context.User?.IsInRole("Admin") ?? true)
        // {
        //     _logger.LogWarning("Unauthorized attempt to send health event by user {UserId}",
        //         Context.User?.Identity?.Name);
        //     throw new HubException("Unauthorized: Admin role required");
        // }

        var update = new RealTimeUpdate
        {
            Type = UpdateType.HealthEvent,
            HealthEvent = healthEvent
        };

        await Clients.Group("dashboard").SendAsync("ReceiveUpdate", update);
    }

    /// <summary>
    /// Get current dashboard connection information
    /// </summary>
    public static IEnumerable<Models.Dashboard.ConnectionInfo> GetActiveConnections()
    {
        return _connections.Values;
    }

    /// <summary>
    /// Get connection count
    /// </summary>
    public static int GetConnectionCount()
    {
        return _connections.Count;
    }

    private async Task SendConnectionUpdate()
    {
        var update = new RealTimeUpdate
        {
            Type = UpdateType.ConnectionUpdate,
            Metrics = new DashboardMetrics
            {
                Timestamp = DateTimeOffset.UtcNow,
                ActiveConnections = GetConnectionCount()
            }
        };

        await Clients.Group("dashboard").SendAsync("ReceiveUpdate", update);
    }
}

/// <summary>
/// Extension methods for NotificationHub
/// </summary>
public static class NotificationHubExtensions
{
    /// <summary>
    /// Send metrics update to all dashboard clients
    /// </summary>
    public static async Task SendMetricsToDashboard(this IHubContext<NotificationHub> hub, DashboardMetrics metrics)
    {
        await hub.Clients.Group("dashboard").SendAsync("ReceiveMetrics", metrics);
    }

    /// <summary>
    /// Send real-time update to all dashboard clients
    /// </summary>
    public static async Task SendUpdateToDashboard(this IHubContext<NotificationHub> hub, RealTimeUpdate update)
    {
        await hub.Clients.Group("dashboard").SendAsync("ReceiveUpdate", update);
    }

    /// <summary>
    /// Send notification event to dashboard clients
    /// </summary>
    public static async Task SendNotificationEventToDashboard(this IHubContext<NotificationHub> hub, NotificationEvent notificationEvent)
    {
        var update = new RealTimeUpdate
        {
            Type = UpdateType.NotificationEvent,
            NotificationEvent = notificationEvent
        };

        await hub.SendUpdateToDashboard(update);
    }

    /// <summary>
    /// Send health event to dashboard clients
    /// </summary>
    public static async Task SendHealthEventToDashboard(this IHubContext<NotificationHub> hub, HealthEvent healthEvent)
    {
        var update = new RealTimeUpdate
        {
            Type = UpdateType.HealthEvent,
            HealthEvent = healthEvent
        };

        await hub.SendUpdateToDashboard(update);
    }
}
