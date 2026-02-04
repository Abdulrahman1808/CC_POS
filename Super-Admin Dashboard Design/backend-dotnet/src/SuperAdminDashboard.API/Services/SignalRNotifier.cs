using Microsoft.AspNetCore.SignalR;
using SuperAdminDashboard.API.Hubs;
using SuperAdminDashboard.Domain.Interfaces;

namespace SuperAdminDashboard.API.Services;

/// <summary>
/// Implements IRealTimeNotifier using SignalR
/// This class lives in API layer and implements the Domain interface
/// </summary>
public sealed class SignalRNotifier : IRealTimeNotifier
{
    private readonly IHubContext<AdminDashboardHub, IAdminDashboardClient> _hubContext;
    private readonly ILogger<SignalRNotifier> _logger;

    public SignalRNotifier(
        IHubContext<AdminDashboardHub, IAdminDashboardClient> hubContext,
        ILogger<SignalRNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyAllAsync(
        string eventName, 
        object payload, 
        CancellationToken ct = default)
    {
        _logger.LogDebug("Broadcasting {EventName} to all clients", eventName);
        
        try
        {
            // Send to all connected admins
            await _hubContext.Clients
                .Group("all-admins")
                .ReceiveNotification(eventName, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast {EventName}", eventName);
            // Don't throw - real-time is best-effort
        }
    }

    public async Task NotifyTenantAsync(
        Guid tenantId, 
        string eventName, 
        object payload, 
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Broadcasting {EventName} to tenant {TenantId}", 
            eventName, tenantId);
        
        try
        {
            await _hubContext.Clients
                .Group($"tenant:{tenantId}")
                .ReceiveNotification(eventName, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to broadcast {EventName} to tenant {TenantId}", 
                eventName, tenantId);
        }
    }

    public async Task NotifyUserAsync(
        string userId, 
        string eventName, 
        object payload, 
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Broadcasting {EventName} to user {UserId}", 
            eventName, userId);
        
        try
        {
            await _hubContext.Clients
                .Group($"user:{userId}")
                .ReceiveNotification(eventName, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to broadcast {EventName} to user {UserId}", 
                eventName, userId);
        }
    }
}
