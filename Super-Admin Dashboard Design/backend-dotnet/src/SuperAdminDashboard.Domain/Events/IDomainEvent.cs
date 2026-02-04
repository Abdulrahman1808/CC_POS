using MediatR;

namespace SuperAdminDashboard.Domain.Events;

/// <summary>
/// Base domain event interface
/// </summary>
public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
    Guid? TenantId { get; }
}

/// <summary>
/// Base domain event record
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? TenantId { get; init; }
}
