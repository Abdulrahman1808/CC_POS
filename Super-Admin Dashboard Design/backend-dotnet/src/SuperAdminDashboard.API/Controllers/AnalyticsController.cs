using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperAdminDashboard.Application.Features.Analytics.DTOs;
using SuperAdminDashboard.Domain.Enums;
using SuperAdminDashboard.Infrastructure.Data;

namespace SuperAdminDashboard.API.Controllers;

/// <summary>
/// Analytics and dashboard metrics endpoints
/// </summary>
[Authorize]
public class AnalyticsController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public AnalyticsController(
        ApplicationDbContext context,
        IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>
    /// Get dashboard overview metrics
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Tenant metrics
        var tenantMetrics = new TenantMetricsDto
        {
            Total = await _context.Tenants.CountAsync(),
            Active = await _context.Tenants.CountAsync(t => t.Status == TenantStatus.Active),
            Suspended = await _context.Tenants.CountAsync(t => t.Status == TenantStatus.Suspended),
            Pending = await _context.Tenants.CountAsync(t => t.Status == TenantStatus.Pending),
            NewThisMonth = await _context.Tenants.CountAsync(t => t.CreatedAt >= startOfMonth)
        };

        // User metrics
        var recentLoginThreshold = now.AddDays(-7);
        var userMetrics = new UserMetricsDto
        {
            Total = await _context.Users.CountAsync(),
            RecentLogins = await _context.Users.CountAsync(u => u.LastLoginAt >= recentLoginThreshold)
        };

        // Revenue (MRR from active subscriptions)
        var mrr = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .SumAsync(s => s.Amount);

        var revenueMetrics = new RevenueMetricsDto
        {
            Mrr = mrr
        };

        return Success(new DashboardOverviewDto
        {
            Tenants = tenantMetrics,
            Users = userMetrics,
            Revenue = revenueMetrics
        });
    }

    /// <summary>
    /// Get tenant growth over time
    /// </summary>
    [HttpGet("tenants/growth")]
    public async Task<IActionResult> GetTenantGrowth([FromQuery] GetGrowthQuery query)
    {
        var days = query.Period switch
        {
            "7d" => 7,
            "90d" => 90,
            "1y" => 365,
            _ => 30
        };

        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        
        var tenants = await _context.Tenants
            .Where(t => t.CreatedAt >= startDate)
            .Select(t => new { t.CreatedAt })
            .ToListAsync();

        var growthData = new List<TenantGrowthDataPoint>();
        var cumulativeTotal = await _context.Tenants.CountAsync(t => t.CreatedAt < startDate);

        for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            var newOnDate = tenants.Count(t => t.CreatedAt.Date == date);
            cumulativeTotal += newOnDate;

            growthData.Add(new TenantGrowthDataPoint
            {
                Date = date.ToString("yyyy-MM-dd"),
                NewTenants = newOnDate,
                TotalTenants = cumulativeTotal
            });
        }

        return Success(growthData);
    }

    /// <summary>
    /// Get tenant distribution by status
    /// </summary>
    [HttpGet("tenants/by-status")]
    public async Task<IActionResult> GetTenantsByStatus()
    {
        var distribution = await _context.Tenants
            .GroupBy(t => t.Status)
            .Select(g => new StatusDistributionDto
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync();

        return Success(distribution);
    }

    /// <summary>
    /// Get tenant distribution by plan
    /// </summary>
    [HttpGet("tenants/by-plan")]
    public async Task<IActionResult> GetTenantsByPlan()
    {
        var distribution = await _context.Tenants
            .Include(t => t.Plan)
            .GroupBy(t => new { t.PlanId, PlanName = t.Plan != null ? t.Plan.Name : "No Plan" })
            .Select(g => new PlanDistributionDto
            {
                PlanId = g.Key.PlanId,
                PlanName = g.Key.PlanName,
                Count = g.Count()
            })
            .ToListAsync();

        return Success(distribution);
    }

    /// <summary>
    /// Get revenue metrics
    /// </summary>
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue([FromQuery] GetGrowthQuery query)
    {
        var mrr = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .SumAsync(s => s.Amount);

        // For simplicity, return current MRR
        // In production, track historical revenue data
        return Success(new RevenueChartDto
        {
            Mrr = mrr,
            ChartData = new List<RevenueDataPoint>
            {
                new() { Date = DateTime.UtcNow.ToString("yyyy-MM-dd"), Revenue = mrr }
            }
        });
    }

    /// <summary>
    /// Get recent activity logs
    /// </summary>
    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity([FromQuery] GetActivityQuery query)
    {
        var logs = await _context.AuditLogs
            .Include(l => l.User)
            .Include(l => l.Tenant)
            .OrderByDescending(l => l.CreatedAt)
            .Take(query.Limit)
            .ToListAsync();

        return Success(_mapper.Map<List<AuditLogDto>>(logs));
    }
}
