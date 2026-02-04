namespace SuperAdminDashboard.Domain.Common;

/// <summary>
/// Base entity with common properties
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity that belongs to a tenant
/// </summary>
public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}

/// <summary>
/// Interface for auditable entities
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}

/// <summary>
/// Interface for soft-deletable entities
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
    bool IsDeleted => DeletedAt.HasValue;
}
