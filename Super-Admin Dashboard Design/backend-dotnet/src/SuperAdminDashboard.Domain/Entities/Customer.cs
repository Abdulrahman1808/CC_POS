using SuperAdminDashboard.Domain.Common;

namespace SuperAdminDashboard.Domain.Entities;

/// <summary>
/// Customer belonging to a tenant (for real-time monitoring)
/// </summary>
public class Customer : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Last activity
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastPurchaseAt { get; set; }
    
    // Stats
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    
    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    
    // Computed
    public string FullName => $"{FirstName} {LastName}".Trim();
}
