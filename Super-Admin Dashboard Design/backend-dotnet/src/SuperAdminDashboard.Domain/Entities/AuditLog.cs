using SuperAdminDashboard.Domain.Common;
using SuperAdminDashboard.Domain.Enums;

namespace SuperAdminDashboard.Domain.Entities;

/// <summary>
/// Audit log for tracking all actions
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    
    // Action details
    public AuditAction Action { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    
    // Change data (JSON)
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    
    // Request context
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestId { get; set; }
    
    // Navigation
    public virtual User? User { get; set; }
    public virtual Tenant? Tenant { get; set; }
}
