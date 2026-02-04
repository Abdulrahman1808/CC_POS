using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SuperAdminDashboard.Domain.Enums;
using SuperAdminDashboard.Domain.Interfaces;

namespace SuperAdminDashboard.Infrastructure.Services;

/// <summary>
/// Gets current user info from JWT claims
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    
    public string? Email => User?.FindFirstValue(ClaimTypes.Email);
    
    public UserRole? Role
    {
        get
        {
            var roleStr = User?.FindFirstValue(ClaimTypes.Role) 
                       ?? User?.FindFirstValue("role");
            
            if (string.IsNullOrEmpty(roleStr))
                return null;
            
            return Enum.TryParse<UserRole>(roleStr, true, out var role) ? role : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var tenantIdStr = User?.FindFirstValue("tenant_id");
            return Guid.TryParse(tenantIdStr, out var id) ? id : null;
        }
    }

    public string? SessionId => User?.FindFirstValue("session_id");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
}

/// <summary>
/// Tenant context for multi-tenancy
/// </summary>
public class TenantContext : ITenantContext
{
    private Guid? _tenantId;

    public Guid? TenantId => _tenantId;

    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
    }
}
