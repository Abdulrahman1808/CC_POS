using SuperAdminDashboard.Domain.Common;
using SuperAdminDashboard.Domain.Enums;

namespace SuperAdminDashboard.Domain.Entities;

/// <summary>
/// Tenant subscription
/// </summary>
public class Subscription : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    
    // Period
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    // Billing
    public string BillingCycle { get; set; } = "monthly";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    
    // Status
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime? CancelledAt { get; set; }
    
    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Plan Plan { get; set; } = null!;
}
