using SuperAdminDashboard.Domain.Common;

namespace SuperAdminDashboard.Domain.Entities;

/// <summary>
/// User session for JWT token management
/// </summary>
public class Session : BaseEntity
{
    public Guid UserId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    // Navigation
    public virtual User User { get; set; } = null!;
    
    // Computed
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
}
