using SuperAdminDashboard.Domain.Common;
using SuperAdminDashboard.Domain.Enums;

namespace SuperAdminDashboard.Domain.Entities;

/// <summary>
/// Admin dashboard user (Super Admin, Admin, Viewer)
/// </summary>
public class User : BaseEntity, IAuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public UserRole Role { get; set; } = UserRole.Viewer;
    public UserStatus Status { get; set; } = UserStatus.Active;
    
    // Email verification
    public DateTime? EmailVerifiedAt { get; set; }
    
    // Login tracking
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    
    // MFA
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }
    
    // Profile
    public string? AvatarUrl { get; set; }
    
    // Audit
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Navigation
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    
    // Computed
    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool IsLocked => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;
}
