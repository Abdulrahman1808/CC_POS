using SuperAdminDashboard.Domain.Enums;

namespace SuperAdminDashboard.Application.Features.Tenants.DTOs;

// ============================================
// Request DTOs
// ============================================

public record CreateTenantRequest
{
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Domain { get; init; }
    public string? LogoUrl { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public Guid? PlanId { get; init; }
    public Dictionary<string, object>? Settings { get; init; }
}

public record UpdateTenantRequest
{
    public string? Name { get; init; }
    public string? Slug { get; init; }
    public string? Domain { get; init; }
    public string? LogoUrl { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public Guid? PlanId { get; init; }
    public Dictionary<string, object>? Settings { get; init; }
}

public record UpdateTenantStatusRequest
{
    public required TenantStatus Status { get; init; }
}

public record GetTenantsQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public TenantStatus? Status { get; init; }
    public string? Search { get; init; }
    public string SortBy { get; init; } = "CreatedAt";
    public bool SortDescending { get; init; } = true;
}

// ============================================
// Response DTOs
// ============================================

public record TenantDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Domain { get; init; }
    public string? LogoUrl { get; init; }
    public TenantStatus Status { get; init; }
    public Guid? PlanId { get; init; }
    public string? PlanName { get; init; }
    public string? ContactEmail { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record TenantDetailDto : TenantDto
{
    public string? ContactPhone { get; init; }
    public string? Settings { get; init; }
    public string? Metadata { get; init; }
    public int CustomersCount { get; init; }
    public int SubscriptionsCount { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record PlanDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public string? Description { get; init; }
    public decimal? PriceMonthly { get; init; }
    public decimal? PriceYearly { get; init; }
    public bool IsActive { get; init; }
}

public record SubscriptionDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid PlanId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string BillingCycle { get; init; } = "monthly";
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public SubscriptionStatus Status { get; init; }
}
