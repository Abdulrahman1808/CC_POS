namespace SuperAdminDashboard.Domain.Interfaces;

/// <summary>
/// Abstraction for real-time notifications.
/// Allows Application layer to broadcast without knowing about SignalR.
/// </summary>
public interface IRealTimeNotifier
{
    /// <summary>
    /// Send notification to all connected admin dashboard clients
    /// </summary>
    Task NotifyAllAsync(string eventName, object payload, CancellationToken ct = default);
    
    /// <summary>
    /// Send notification to specific tenant's dashboard
    /// </summary>
    Task NotifyTenantAsync(Guid tenantId, string eventName, object payload, CancellationToken ct = default);
    
    /// <summary>
    /// Send notification to specific user
    /// </summary>
    Task NotifyUserAsync(string userId, string eventName, object payload, CancellationToken ct = default);
}
