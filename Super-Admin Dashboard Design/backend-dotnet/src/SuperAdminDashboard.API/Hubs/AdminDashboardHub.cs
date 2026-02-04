using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SuperAdminDashboard.Domain.Interfaces;

namespace SuperAdminDashboard.API.Hubs;

/// <summary>
/// SignalR Hub for real-time admin dashboard updates
/// </summary>
[Authorize]
public sealed class AdminDashboardHub : Hub<IAdminDashboardClient>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AdminDashboardHub> _logger;

    public AdminDashboardHub(
        ICurrentUserService currentUser,
        ILogger<AdminDashboardHub> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUser.UserId;
        var tenantId = _currentUser.TenantId;
        
        _logger.LogInformation(
            "Admin connected: UserId={UserId}, TenantId={TenantId}, ConnectionId={ConnectionId}",
            userId, tenantId, Context.ConnectionId);

        // Add to user-specific group
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        // Add to tenant-specific group (for tenant admins)
        if (tenantId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
        }

        // Add to super-admin group if applicable
        if (_currentUser.IsSuperAdmin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "super-admins");
        }

        // Add to all-admins group
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-admins");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Admin disconnected: ConnectionId={ConnectionId}, Error={Error}",
            Context.ConnectionId, exception?.Message);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client can subscribe to specific event types
    /// </summary>
    public async Task SubscribeToEvents(string[] eventTypes)
    {
        foreach (var eventType in eventTypes)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"event:{eventType}");
            _logger.LogDebug("Client {ConnectionId} subscribed to {EventType}", 
                Context.ConnectionId, eventType);
        }
    }

    /// <summary>
    /// Client can unsubscribe from events
    /// </summary>
    public async Task UnsubscribeFromEvents(string[] eventTypes)
    {
        foreach (var eventType in eventTypes)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event:{eventType}");
        }
    }

    /// <summary>
    /// Ping to keep connection alive
    /// </summary>
    public Task Ping()
    {
        return Task.CompletedTask;
    }
}
