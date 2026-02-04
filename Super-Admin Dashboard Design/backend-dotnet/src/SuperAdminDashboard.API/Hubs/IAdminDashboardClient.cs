namespace SuperAdminDashboard.API.Hubs;

/// <summary>
/// Strongly-typed contract for client-side methods that can be called from server
/// </summary>
public interface IAdminDashboardClient
{
    // Customer Events
    Task CustomerLoggedIn(object data);
    Task CustomerPurchased(object data);
    Task CustomerRegistered(object data);
    
    // System Events
    Task SystemError(object data);
    Task SystemAlert(object data);
    
    // Tenant Events
    Task TenantStatusChanged(object data);
    Task TenantCreated(object data);
    
    // Metrics Updates
    Task MetricsUpdated(object data);
    
    // Generic notification
    Task ReceiveNotification(string eventName, object data);
}
