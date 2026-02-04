using SuperAdminDashboard.Domain.Enums;

namespace SuperAdminDashboard.Domain.Interfaces;

/// <summary>
/// Current user context from JWT token
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    Guid? TenantId { get; }
    string? SessionId { get; }
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }
}

/// <summary>
/// Tenant context for multi-tenancy
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
    void SetTenant(Guid tenantId);
}
