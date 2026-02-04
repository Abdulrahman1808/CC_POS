using SuperAdminDashboard.Domain.Common;

namespace SuperAdminDashboard.Domain.Entities;

/// <summary>
/// API Key for tenant integrations
/// </summary>
public class ApiKey : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    
    // Permissions (JSON)
    public string Scopes { get; set; } = "[]";
    
    // Usage
    public DateTime? LastUsedAt { get; set; }
    public int UsageCount { get; set; }
    
    // Expiration
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    
    // Computed
    public bool IsActive => RevokedAt == null && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
}
