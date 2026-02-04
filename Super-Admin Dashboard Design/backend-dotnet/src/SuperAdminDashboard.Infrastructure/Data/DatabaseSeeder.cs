using Microsoft.EntityFrameworkCore;
using SuperAdminDashboard.Domain.Entities;
using SuperAdminDashboard.Domain.Enums;
using SuperAdminDashboard.Infrastructure.Services;

namespace SuperAdminDashboard.Infrastructure.Data;

/// <summary>
/// Database seeder for initial data
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordService passwordService)
    {
        // Seed Plans
        if (!await context.Plans.AnyAsync())
        {
            var plans = new List<Plan>
            {
                new()
                {
                    Name = "Free",
                    Code = "free",
                    Description = "Basic plan for small businesses",
                    PriceMonthly = 0,
                    PriceYearly = 0,
                    Features = "[\"Up to 100 transactions/month\", \"Basic reporting\", \"Email support\"]",
                    Limits = "{\"transactions\": 100, \"users\": 2, \"locations\": 1}",
                    SortOrder = 1,
                    IsActive = true
                },
                new()
                {
                    Name = "Starter",
                    Code = "starter",
                    Description = "For growing businesses",
                    PriceMonthly = 29,
                    PriceYearly = 290,
                    Features = "[\"Up to 1,000 transactions/month\", \"Advanced reporting\", \"Priority email support\", \"API access\"]",
                    Limits = "{\"transactions\": 1000, \"users\": 5, \"locations\": 3}",
                    SortOrder = 2,
                    IsActive = true
                },
                new()
                {
                    Name = "Professional",
                    Code = "professional",
                    Description = "For established businesses",
                    PriceMonthly = 99,
                    PriceYearly = 990,
                    Features = "[\"Unlimited transactions\", \"Custom reporting\", \"Phone support\", \"API access\", \"Integrations\"]",
                    Limits = "{\"transactions\": -1, \"users\": 20, \"locations\": 10}",
                    SortOrder = 3,
                    IsActive = true
                },
                new()
                {
                    Name = "Enterprise",
                    Code = "enterprise",
                    Description = "For large organizations",
                    PriceMonthly = 299,
                    PriceYearly = 2990,
                    Features = "[\"Unlimited everything\", \"Dedicated support\", \"Custom integrations\", \"SLA\", \"White-label\"]",
                    Limits = "{\"transactions\": -1, \"users\": -1, \"locations\": -1}",
                    SortOrder = 4,
                    IsActive = true
                }
            };

            await context.Plans.AddRangeAsync(plans);
            await context.SaveChangesAsync();
        }

        // Seed Super Admin user
        if (!await context.Users.AnyAsync())
        {
            var superAdmin = new User
            {
                Email = "admin@superadmin.com",
                PasswordHash = passwordService.HashPassword("SuperAdmin@123!"),
                FirstName = "Super",
                LastName = "Admin",
                Role = UserRole.SuperAdmin,
                Status = UserStatus.Active,
                EmailVerifiedAt = DateTime.UtcNow
            };

            await context.Users.AddAsync(superAdmin);
            await context.SaveChangesAsync();
        }

        // Seed sample tenants
        if (!await context.Tenants.AnyAsync())
        {
            var plans = await context.Plans.ToListAsync();
            var professionalPlan = plans.FirstOrDefault(p => p.Code == "professional");
            var starterPlan = plans.FirstOrDefault(p => p.Code == "starter");
            var freePlan = plans.FirstOrDefault(p => p.Code == "free");

            var tenants = new List<Tenant>
            {
                new()
                {
                    Name = "Acme Corporation",
                    Slug = "acme-corp",
                    Domain = "acme.example.com",
                    ContactEmail = "contact@acme.example.com",
                    Status = TenantStatus.Active,
                    PlanId = professionalPlan?.Id,
                    Settings = "{\"timezone\": \"America/New_York\", \"currency\": \"USD\"}"
                },
                new()
                {
                    Name = "Globex Inc",
                    Slug = "globex-inc",
                    Domain = "globex.example.com",
                    ContactEmail = "info@globex.example.com",
                    Status = TenantStatus.Active,
                    PlanId = starterPlan?.Id,
                    Settings = "{\"timezone\": \"Europe/London\", \"currency\": \"GBP\"}"
                },
                new()
                {
                    Name = "Initech",
                    Slug = "initech",
                    ContactEmail = "support@initech.example.com",
                    Status = TenantStatus.Pending,
                    PlanId = freePlan?.Id,
                    Settings = "{\"timezone\": \"America/Los_Angeles\", \"currency\": \"USD\"}"
                }
            };

            await context.Tenants.AddRangeAsync(tenants);
            await context.SaveChangesAsync();

            // Create subscriptions for tenants
            foreach (var tenant in tenants)
            {
                if (tenant.PlanId.HasValue)
                {
                    var plan = plans.First(p => p.Id == tenant.PlanId);
                    var subscription = new Subscription
                    {
                        TenantId = tenant.Id,
                        PlanId = tenant.PlanId.Value,
                        StartDate = DateTime.UtcNow,
                        BillingCycle = "monthly",
                        Amount = plan.PriceMonthly ?? 0,
                        Currency = "USD",
                        Status = SubscriptionStatus.Active
                    };
                    await context.Subscriptions.AddAsync(subscription);
                }
            }
            await context.SaveChangesAsync();
        }

        // Seed system settings
        if (!await context.SystemSettings.AnyAsync())
        {
            var settings = new List<SystemSetting>
            {
                new() { Key = "app_name", Value = "Super Admin Dashboard", Description = "Application display name", Category = "general" },
                new() { Key = "maintenance_mode", Value = "false", Description = "Enable maintenance mode", Category = "general" },
                new() { Key = "max_login_attempts", Value = "5", Description = "Maximum failed login attempts before lockout", Category = "security" },
                new() { Key = "session_timeout_minutes", Value = "60", Description = "Session timeout in minutes", Category = "security" }
            };

            await context.SystemSettings.AddRangeAsync(settings);
            await context.SaveChangesAsync();
        }
    }
}
