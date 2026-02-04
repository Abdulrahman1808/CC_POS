using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data.Interfaces;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.Data;

/// <summary>
/// SQLite implementation of IDataService with automatic sync queue tracking.
/// Uses IDbContextFactory to create fresh contexts per operation for thread safety.
/// Auto-populates BusinessId from TenantContext for multi-tenant support.
/// </summary>
public class SqliteDataService : IDataService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ITenantContext _tenantContext;

    public SqliteDataService(
        IDbContextFactory<AppDbContext> contextFactory,
        ITenantContext tenantContext)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }


    #region Products

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Products
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(Guid id)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<Product?> GetProductByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        
        using var context = _contextFactory.CreateDbContext();
        return await context.Products
            .FirstOrDefaultAsync(p => p.Barcode == barcode && !p.IsDeleted);
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllProductsAsync();

        using var context = _contextFactory.CreateDbContext();
        var term = searchTerm.ToLower();
        return await context.Products
            .Where(p => !p.IsDeleted && 
                       (p.Name.ToLower().Contains(term) || 
                        (p.Barcode != null && p.Barcode.Contains(term)) ||
                        (p.Category != null && p.Category.ToLower().Contains(term))))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Products
            .Where(p => !p.IsDeleted && p.Category == category)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Products
            .Where(p => !p.IsDeleted && p.Category != null)
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<bool> AddProductAsync(Product product)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            
            // Auto-populate BusinessId and BranchId for multi-tenant isolation
            if (_tenantContext.IsContextValid && product.BusinessId == null)
            {
                product.BusinessId = _tenantContext.CurrentBusinessId;
            }
            if (_tenantContext.IsBranchSelected && product.BranchId == null)
            {
                product.BranchId = _tenantContext.CurrentBranchId;
            }
            
            await context.Products.AddAsync(product);
            await context.SaveChangesAsync();

            // Add to sync queue
            await AddToSyncQueueAsync(new SyncRecord
            {
                EntityType = nameof(Product),
                EntityId = product.Id,
                Operation = SyncOperation.Create,
                Payload = JsonSerializer.Serialize(product)
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var existing = await context.Products.FindAsync(product.Id);
            if (existing == null) return false;

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.Barcode = product.Barcode;
            existing.Category = product.Category;
            existing.StockQuantity = product.StockQuantity;
            existing.ImagePath = product.ImagePath;
            existing.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            // Add to sync queue
            await AddToSyncQueueAsync(new SyncRecord
            {
                EntityType = nameof(Product),
                EntityId = product.Id,
                Operation = SyncOperation.Update,
                Payload = JsonSerializer.Serialize(existing)
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                Debug.WriteLine($"[SqliteDataService] DeleteProduct: Product {id} not found");
                return false;
            }

            // Soft delete
            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;
            
            context.Products.Update(product);
            var changes = await context.SaveChangesAsync();
            
            Debug.WriteLine($"[SqliteDataService] DeleteProduct: {product.Name} - IsDeleted={product.IsDeleted}, SavedChanges={changes}");

            // Add to sync queue
            await AddToSyncQueueAsync(new SyncRecord
            {
                EntityType = nameof(Product),
                EntityId = id,
                Operation = SyncOperation.Delete
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SqliteDataService] DeleteProduct ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Transactions

    public async Task<IEnumerable<Transaction>> GetTransactionsAsync(DateTime? from = null, DateTime? to = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Transactions
            .Include(t => t.Items)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Transaction?> GetTransactionByIdAsync(Guid id)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Transactions
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transaction?> GetTransactionByNumberAsync(string transactionNumber)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Transactions
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.TransactionNumber == transactionNumber);
    }

    public async Task<bool> UpdateTransactionAsync(Transaction transaction)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            context.Transactions.Update(transaction);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SqliteDataService] UpdateTransaction ERROR: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateTransactionAsync(Transaction transaction)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            Debug.WriteLine($"[CreateTransaction] Starting for {transaction.Items?.Count ?? 0} items...");
            
            // Generate transaction number if not provided
            if (string.IsNullOrEmpty(transaction.TransactionNumber))
            {
                transaction.TransactionNumber = await GenerateTransactionNumberAsync();
            }

            transaction.CreatedAt = DateTime.UtcNow;
            transaction.CalculateTotals();
            
            // Auto-populate BusinessId and BranchId for multi-tenant isolation
            if (_tenantContext.IsContextValid && transaction.BusinessId == null)
            {
                transaction.BusinessId = _tenantContext.CurrentBusinessId;
            }
            if (_tenantContext.IsBranchSelected && transaction.BranchId == null)
            {
                transaction.BranchId = _tenantContext.CurrentBranchId;
            }
            
            Debug.WriteLine($"[CreateTransaction] Total: {transaction.Total}, Number: {transaction.TransactionNumber}, BusinessId: {transaction.BusinessId}, BranchId: {transaction.BranchId}");


            await context.Transactions.AddAsync(transaction);
            await context.SaveChangesAsync();
            
            Debug.WriteLine($"[CreateTransaction] Saved successfully!");

            // Add to sync queue
            await AddToSyncQueueAsync(new SyncRecord
            {
                EntityType = nameof(Transaction),
                EntityId = transaction.Id,
                Operation = SyncOperation.Create,
                Payload = JsonSerializer.Serialize(transaction)
            });

            return true;
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? "No inner exception";
            Debug.WriteLine($"[CreateTransaction] ERROR: {ex.Message}");
            Debug.WriteLine($"[CreateTransaction] INNER: {innerMsg}");
            Debug.WriteLine($"[CreateTransaction] Stack: {ex.StackTrace}");
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(
                    $"Database Error:\n\n{ex.Message}\n\nInner: {innerMsg}",
                    "Transaction Save Failed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            });
            
            return false;
        }
    }

    public async Task<IEnumerable<Transaction>> GetTodayTransactionsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        
        return await GetTransactionsAsync(today, tomorrow);
    }

    public async Task<decimal> GetDailySalesTotalAsync(DateTime date)
    {
        using var context = _contextFactory.CreateDbContext();
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);
        
        var totals = await context.Transactions
            .Where(t => t.CreatedAt >= startOfDay && t.CreatedAt < endOfDay)
            .Select(t => t.Total)
            .ToListAsync();
        
        return totals.Sum();
    }

    private async Task<string> GenerateTransactionNumberAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        
        var count = await context.Transactions
            .Where(t => t.CreatedAt >= today && t.CreatedAt < tomorrow)
            .CountAsync();

        return $"TXN-{today:yyyyMMdd}-{(count + 1):D4}";
    }

    #endregion

    #region Sync Queue

    public async Task<IEnumerable<SyncRecord>> GetPendingSyncRecordsAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.SyncRecords
            .Where(s => s.Status == SyncStatus.Pending || s.Status == SyncStatus.Failed)
            .Where(s => s.RetryCount < 5)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetPendingSyncCountAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.SyncRecords
            .Where(s => s.Status == SyncStatus.Pending || s.Status == SyncStatus.Failed)
            .Where(s => s.RetryCount < 5)
            .CountAsync();
    }

    public async Task<bool> MarkAsSyncedAsync(Guid syncRecordId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var record = await context.SyncRecords.FindAsync(syncRecordId);
            if (record == null) return false;

            record.Status = SyncStatus.Completed;
            record.SyncedAt = DateTime.UtcNow;
            
            await context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> MarkSyncFailedAsync(Guid syncRecordId, string errorMessage)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var record = await context.SyncRecords.FindAsync(syncRecordId);
            if (record == null) return false;

            record.Status = SyncStatus.Failed;
            record.RetryCount++;
            record.ErrorMessage = errorMessage;
            
            await context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AddToSyncQueueAsync(SyncRecord record)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            await context.SyncRecords.AddAsync(record);
            await context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> ClearOldSyncRecordsAsync(DateTime olderThan)
    {
        using var context = _contextFactory.CreateDbContext();
        var oldRecords = await context.SyncRecords
            .Where(s => s.Status == SyncStatus.Completed && s.SyncedAt < olderThan)
            .ToListAsync();

        context.SyncRecords.RemoveRange(oldRecords);
        await context.SaveChangesAsync();
        
        return oldRecords.Count;
    }

    #endregion

    #region Staff Members

    public async Task<IEnumerable<StaffMember>> GetActiveStaffMembersAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.StaffMembers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<StaffMember?> GetStaffByPinAsync(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin)) return null;

        using var context = _contextFactory.CreateDbContext();
        return await context.StaffMembers
            .FirstOrDefaultAsync(s => s.Pin == pin && s.IsActive);
    }

    public async Task<int> GetStaffCountAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.StaffMembers
            .Where(s => s.IsActive)
            .CountAsync();
    }

    public async Task<bool> AddStaffMemberAsync(StaffMember staff)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            staff.CreatedAt = DateTime.UtcNow;
            staff.UpdatedAt = DateTime.UtcNow;

            await context.StaffMembers.AddAsync(staff);
            await context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateStaffMemberAsync(StaffMember staff)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var existing = await context.StaffMembers.FindAsync(staff.Id);
            if (existing == null) return false;

            existing.Name = staff.Name;
            existing.Pin = staff.Pin;
            existing.Email = staff.Email;
            existing.Level = staff.Level;
            existing.IsActive = staff.IsActive;
            existing.CanDeleteTransactions = staff.CanDeleteTransactions;
            existing.CanChangePrices = staff.CanChangePrices;
            existing.CanViewReports = staff.CanViewReports;
            existing.CanManageStaff = staff.CanManageStaff;
            existing.CanVoidItems = staff.CanVoidItems;
            existing.CanApplyDiscounts = staff.CanApplyDiscounts;
            existing.CanAccessSettings = staff.CanAccessSettings;
            existing.CanReconcileCashDrawer = staff.CanReconcileCashDrawer;
            existing.LastLoginAt = staff.LastLoginAt;
            existing.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeactivateStaffMemberAsync(Guid id)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var staff = await context.StaffMembers.FindAsync(id);
            if (staff == null) return false;

            staff.IsActive = false;
            staff.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Database Management

    public async Task InitializeDatabaseAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        await context.Database.EnsureCreatedAsync();
        await MigrateSchemaAsync(context);
    }

    public async Task<bool> ClearAllDataAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            // Clear all data using raw SQL for efficiency and to bypass cascade issues
            await context.Database.ExecuteSqlRawAsync("DELETE FROM TransactionItems");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Transactions");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM SyncRecords");
            
            // Note: We don't delete Products or Staff members here as they are part of the setup
            
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SqliteDataService] ClearAllData ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Applies schema migrations for new columns added after initial release.
    /// SQLite doesn't support full ALTER TABLE, so we check if columns exist first.
    /// </summary>
    private async Task MigrateSchemaAsync(AppDbContext context)
    {
        try
        {
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info(Transactions)";
            
            var existingColumns = new HashSet<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    existingColumns.Add(reader.GetString(1));
                }
            }
            
            if (!existingColumns.Contains("PaymentReference"))
            {
                Debug.WriteLine("[Migration] Adding PaymentReference column...");
                using var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Transactions ADD COLUMN PaymentReference TEXT";
                await alterCommand.ExecuteNonQueryAsync();
            }
            
            if (!existingColumns.Contains("CustomerPhone"))
            {
                Debug.WriteLine("[Migration] Adding CustomerPhone column...");
                using var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Transactions ADD COLUMN CustomerPhone TEXT";
                await alterCommand.ExecuteNonQueryAsync();
            }
            
            Debug.WriteLine("[Migration] Schema migration complete.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Migration] Schema migration warning: {ex.Message}");
        }
    }

    #endregion

    #region Activity Log

    public async Task<bool> AddActivityLogAsync(ActivityLog log)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            
            // Auto-populate tenant context
            log.BusinessId ??= _tenantContext.CurrentBusinessId;
            log.BranchId ??= _tenantContext.CurrentBranchId;
            log.StaffId ??= _tenantContext.CurrentStaffId;
            log.StaffName ??= _tenantContext.CurrentStaffName;
            
            await context.ActivityLogs.AddAsync(log);
            await context.SaveChangesAsync();
            
            Debug.WriteLine($"[SqliteDataService] ActivityLog added: {log.Action} - {log.EntityType}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SqliteDataService] AddActivityLog ERROR: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<ActivityLog>> GetActivityLogsAsync(DateTime? from = null, DateTime? to = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.ActivityLogs.AsQueryable();

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Take(1000)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetUnsyncedActivityLogsAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.ActivityLogs
            .Where(l => !l.IsSynced)
            .OrderBy(l => l.Timestamp)
            .Take(100)
            .ToListAsync();
    }

    public async Task<bool> MarkActivityLogsSyncedAsync(IEnumerable<Guid> ids)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var logs = await context.ActivityLogs
                .Where(l => ids.Contains(l.Id))
                .ToListAsync();

            foreach (var log in logs)
            {
                log.IsSynced = true;
            }

            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SqliteDataService] MarkActivityLogsSynced ERROR: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<StaffMember>> GetStaffMembersAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.StaffMembers
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    #endregion
}
