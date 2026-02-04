using SuperAdminDashboard.Domain.Common;

namespace SuperAdminDashboard.Domain.Entities;

/// <summary>
/// Subscription plan
/// </summary>
public class Plan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Pricing
    public decimal? PriceMonthly { get; set; }
    public decimal? PriceYearly { get; set; }
    
    // Features & Limits (JSON)
    public string Features { get; set; } = "[]";
    public string Limits { get; set; } = "{}";
    
    // Status
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    
    // Navigation
    public virtual ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
