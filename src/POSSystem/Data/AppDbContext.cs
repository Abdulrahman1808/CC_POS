using Microsoft.EntityFrameworkCore;
using POSSystem.Models;

namespace POSSystem.Data;

/// <summary>
/// Entity Framework Core DbContext for SQLite database.
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionItem> TransactionItems { get; set; }
    public DbSet<SyncRecord> SyncRecords { get; set; }
    public DbSet<StaffMember> StaffMembers { get; set; }
    public DbSet<AppSettings> Settings { get; set; }
    public DbSet<BusinessProfile> BusinessProfiles { get; set; }
    public DbSet<StoreSettings> StoreSettings { get; set; }
    public DbSet<HardwareSettings> HardwareSettings { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Cost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxRate).HasColumnType("decimal(5,4)");
            entity.HasIndex(e => e.Barcode);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsDeleted);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxRate).HasColumnType("decimal(5,4)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.TransactionNumber).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsSynced);
        });

        // TransactionItem configuration
        modelBuilder.Entity<TransactionItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Transaction)
                  .WithMany(t => t.Items)
                  .HasForeignKey(e => e.TransactionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SyncRecord configuration
        modelBuilder.Entity<SyncRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
        });

        // StaffMember configuration
        modelBuilder.Entity<StaffMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Pin);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Level);
        });
    }
}
