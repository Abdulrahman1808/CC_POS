using Microsoft.EntityFrameworkCore;
using SuperAdminDashboard.Domain.Common;
using SuperAdminDashboard.Domain.Entities;
using SuperAdminDashboard.Domain.Interfaces;

namespace SuperAdminDashboard.Infrastructure.Data;

/// <summary>
/// Application database context with tenant isolation
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext? tenantContext = null,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    // DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filter for tenant isolation (for Customer entity)
        if (_tenantContext?.TenantId != null)
        {
            modelBuilder.Entity<Customer>()
                .HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        }

        // Soft delete filter
        modelBuilder.Entity<Tenant>()
            .HasQueryFilter(t => t.DeletedAt == null);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        // Update auditable entities with user info
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            var userId = _currentUserService?.UserId;
            
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
