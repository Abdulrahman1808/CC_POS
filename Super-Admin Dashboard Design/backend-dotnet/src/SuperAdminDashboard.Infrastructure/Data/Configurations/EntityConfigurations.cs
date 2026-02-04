using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAdminDashboard.Domain.Entities;

namespace SuperAdminDashboard.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.FirstName)
            .HasMaxLength(50);

        builder.Property(u => u.LastName)
            .HasMaxLength(50);

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasMany(u => u.Sessions)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.AuditLogs)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.RefreshTokenHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.IpAddress)
            .HasMaxLength(45);

        builder.Property(s => s.UserAgent)
            .HasMaxLength(500);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.ExpiresAt);
    }
}

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(t => t.Slug)
            .IsUnique();

        builder.Property(t => t.Domain)
            .HasMaxLength(255);
        
        builder.HasIndex(t => t.Domain)
            .IsUnique()
            .HasFilter("domain IS NOT NULL");

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Settings)
            .HasColumnType("jsonb");

        builder.Property(t => t.Metadata)
            .HasColumnType("jsonb");

        builder.HasOne(t => t.Plan)
            .WithMany(p => p.Tenants)
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.Property(p => p.PriceMonthly)
            .HasPrecision(10, 2);

        builder.Property(p => p.PriceYearly)
            .HasPrecision(10, 2);

        builder.Property(p => p.Features)
            .HasColumnType("jsonb");

        builder.Property(p => p.Limits)
            .HasColumnType("jsonb");
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.BillingCycle)
            .HasMaxLength(20);

        builder.Property(s => s.Amount)
            .HasPrecision(10, 2);

        builder.Property(s => s.Currency)
            .HasMaxLength(3);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(s => s.Tenant)
            .WithMany(t => t.Subscriptions)
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.FirstName)
            .HasMaxLength(50);

        builder.Property(c => c.LastName)
            .HasMaxLength(50);

        builder.Property(c => c.TotalSpent)
            .HasPrecision(10, 2);

        builder.HasIndex(c => new { c.TenantId, c.Email })
            .IsUnique();

        builder.HasOne(c => c.Tenant)
            .WithMany(t => t.Customers)
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.KeyHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.KeyPrefix)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(a => a.Scopes)
            .HasColumnType("jsonb");

        builder.HasOne(a => a.Tenant)
            .WithMany(t => t.ApiKeys)
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(s => s.Key)
            .IsUnique();

        builder.Property(s => s.Value)
            .IsRequired();

        builder.Property(s => s.Category)
            .HasMaxLength(50);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.ResourceType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.ResourceId)
            .HasMaxLength(50);

        builder.Property(a => a.OldValues)
            .HasColumnType("jsonb");

        builder.Property(a => a.NewValues)
            .HasColumnType("jsonb");

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45);

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.CreatedAt);
    }
}
