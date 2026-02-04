using SuperAdminDashboard.Domain.Enums;

namespace SuperAdminDashboard.Application.Features.Analytics.DTOs;

// ============================================
// Dashboard Overview
// ============================================

public record DashboardOverviewDto
{
    public required TenantMetricsDto Tenants { get; init; }
    public required UserMetricsDto Users { get; init; }
    public required RevenueMetricsDto Revenue { get; init; }
}

public record TenantMetricsDto
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Suspended { get; init; }
    public int Pending { get; init; }
    public int NewThisMonth { get; init; }
}

public record UserMetricsDto
{
    public int Total { get; init; }
    public int RecentLogins { get; init; }
}

public record RevenueMetricsDto
{
    public decimal Mrr { get; init; }
}

// ============================================
// Charts & Time Series
// ============================================

public record TenantGrowthDataPoint
{
    public required string Date { get; init; }
    public int NewTenants { get; init; }
    public int TotalTenants { get; init; }
}

public record StatusDistributionDto
{
    public required string Status { get; init; }
    public int Count { get; init; }
}

public record PlanDistributionDto
{
    public Guid? PlanId { get; init; }
    public required string PlanName { get; init; }
    public int Count { get; init; }
}

public record RevenueDataPoint
{
    public required string Date { get; init; }
    public decimal Revenue { get; init; }
}

public record RevenueChartDto
{
    public decimal Mrr { get; init; }
    public IReadOnlyList<RevenueDataPoint> ChartData { get; init; } = Array.Empty<RevenueDataPoint>();
}

// ============================================
// Activity & Audit
// ============================================

public record AuditLogDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? UserEmail { get; init; }
    public Guid? TenantId { get; init; }
    public string? TenantName { get; init; }
    public AuditAction Action { get; init; }
    public required string ResourceType { get; init; }
    public string? ResourceId { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public DateTime CreatedAt { get; init; }
}

// ============================================
// Query Parameters
// ============================================

public record GetGrowthQuery
{
    public string Period { get; init; } = "30d"; // 7d, 30d, 90d, 1y
}

public record GetActivityQuery
{
    public int Limit { get; init; } = 20;
}
