using SuperAdminDashboard.Domain.Common;

namespace SuperAdminDashboard.Domain.Entities;

/// <summary>
/// System settings
/// </summary>
public class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "general";
    public bool IsSensitive { get; set; }
    
    // Tracking
    public Guid? UpdatedById { get; set; }
    
    // Navigation
    public virtual User? UpdatedBy { get; set; }
}
