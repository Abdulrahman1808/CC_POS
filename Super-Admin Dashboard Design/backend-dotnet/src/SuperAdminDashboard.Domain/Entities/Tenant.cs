using SuperAdminDashboard.Domain.Common;
using SuperAdminDashboard.Domain.Enums;

namespace SuperAdminDashboard.Domain.Entities;

/// <summary>
/// Tenant/Client organization
/// </summary>
public class Tenant : BaseEntity, IAuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? LogoUrl { get; set; }
    
    // Status & Plan
    public TenantStatus Status { get; set; } = TenantStatus.Pending;
    public Guid? PlanId { get; set; }
    
    // Contact
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    
    // Settings (JSON)
    public string? Settings { get; set; }
    public string? Metadata { get; set; }
    
    // Audit
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Soft delete
    public DateTime? DeletedAt { get; set; }
    
    // Navigation
    public virtual Plan? Plan { get; set; }
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
