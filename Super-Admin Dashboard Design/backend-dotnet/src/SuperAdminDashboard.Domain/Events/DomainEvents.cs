namespace SuperAdminDashboard.Domain.Events;

/// <summary>
/// Event: Customer logged in
/// </summary>
public sealed record CustomerLoggedInEvent : DomainEventBase
{
    public required Guid CustomerId { get; init; }
    public required string CustomerEmail { get; init; }
    public required string? IpAddress { get; init; }
}

/// <summary>
/// Event: Customer made a purchase
/// </summary>
public sealed record CustomerPurchasedEvent : DomainEventBase
{
    public required Guid CustomerId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
}

/// <summary>
/// Event: New customer registered
/// </summary>
public sealed record CustomerRegisteredEvent : DomainEventBase
{
    public required Guid CustomerId { get; init; }
    public required string CustomerEmail { get; init; }
}

/// <summary>
/// Event: System error occurred
/// </summary>
public sealed record SystemErrorEvent : DomainEventBase
{
    public required string ErrorCode { get; init; }
    public required string Message { get; init; }
    public required string Source { get; init; }
    public string? StackTrace { get; init; }
}

/// <summary>
/// Event: Tenant status changed
/// </summary>
public sealed record TenantStatusChangedEvent : DomainEventBase
{
    public required string OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public required string TenantName { get; init; }
}

/// <summary>
/// Event: User logged in to dashboard
/// </summary>
public sealed record UserLoggedInEvent : DomainEventBase
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string? IpAddress { get; init; }
}
