using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data.Interfaces;
using POSSystem.Models;

namespace POSSystem.Data;

/// <summary>
/// SQLite implementation of IDataService with automatic sync queue tracking.
/// All operations are async to prevent UI blocking.
/// </summary>
public class SqliteDataService : IDataService
{
    private readonly AppDbContext _context;

    public SqliteDataService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Products

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _context.Products
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(Guid id)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<Product?> GetProductByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Barcode == barcode && !p.IsDeleted);
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllProductsAsync();

        var term = searchTerm.ToLower();
        return await _context.Products
            .Where(p => !p.IsDeleted && 
                       (p.Name.ToLower().Contains(term) || 
                        (p.Barcode != null && p.Barcode.Contains(term)) ||
                        (p.Category != null && p.Category.ToLower().Contains(term))))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
    {
        return await _context.Products
            .Where(p => !p.IsDeleted && p.Category == category)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        return await _context.Products
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
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

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
            var existing = await _context.Products.FindAsync(product.Id);
            if (existing == null) return false;

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.Barcode = product.Barcode;
            existing.Category = product.Category;
            existing.StockQuantity = product.StockQuantity;
            existing.ImagePath = product.ImagePath;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                Debug.WriteLine($"[SqliteDataService] DeleteProduct: Product {id} not found");
                return false;
            }

            // Soft delete - explicitly mark entity as modified
            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;
            
            _context.Products.Update(product); // Explicit update to ensure EF tracks changes
            var changes = await _context.SaveChangesAsync();
            
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
        var query = _context.Transactions
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
        return await _context.Transactions
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transaction?> GetTransactionByNumberAsync(string transactionNumber)
    {
        return await _context.Transactions
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.TransactionNumber == transactionNumber);
    }

    public async Task<bool> CreateTransactionAsync(Transaction transaction)
    {
        try
        {
            Debug.WriteLine($"[CreateTransaction] Starting for {transaction.Items?.Count ?? 0} items...");
            
            // Generate transaction number if not provided
            if (string.IsNullOrEmpty(transaction.TransactionNumber))
            {
                transaction.TransactionNumber = await GenerateTransactionNumberAsync();
            }

            transaction.CreatedAt = DateTime.UtcNow;
            transaction.CalculateTotals();
            
            Debug.WriteLine($"[CreateTransaction] Total: {transaction.Total}, Number: {transaction.TransactionNumber}");

            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            
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
            
            // Show actual error to user for debugging
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
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);
        
        // SQLite doesn't support Sum on decimal, so we load data first
        var totals = await _context.Transactions
            .Where(t => t.CreatedAt >= startOfDay && t.CreatedAt < endOfDay)
            .Select(t => t.Total)
            .ToListAsync();
        
        return totals.Sum();
    }

    private async Task<string> GenerateTransactionNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        
        var count = await _context.Transactions
            .Where(t => t.CreatedAt >= today && t.CreatedAt < tomorrow)
            .CountAsync();

        return $"TXN-{today:yyyyMMdd}-{(count + 1):D4}";
    }

    #endregion

    #region Sync Queue

    public async Task<IEnumerable<SyncRecord>> GetPendingSyncRecordsAsync()
    {
        return await _context.SyncRecords
            .Where(s => s.Status == SyncStatus.Pending || s.Status == SyncStatus.Failed)
            .Where(s => s.RetryCount < 5) // Max 5 retries
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetPendingSyncCountAsync()
    {
        return await _context.SyncRecords
            .Where(s => s.Status == SyncStatus.Pending || s.Status == SyncStatus.Failed)
            .Where(s => s.RetryCount < 5)
            .CountAsync();
    }

    public async Task<bool> MarkAsSyncedAsync(Guid syncRecordId)
    {
        try
        {
            var record = await _context.SyncRecords.FindAsync(syncRecordId);
            if (record == null) return false;

            record.Status = SyncStatus.Completed;
            record.SyncedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
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
            var record = await _context.SyncRecords.FindAsync(syncRecordId);
            if (record == null) return false;

            record.Status = SyncStatus.Failed;
            record.RetryCount++;
            record.ErrorMessage = errorMessage;
            
            await _context.SaveChangesAsync();
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
            await _context.SyncRecords.AddAsync(record);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> ClearOldSyncRecordsAsync(DateTime olderThan)
    {
        var oldRecords = await _context.SyncRecords
            .Where(s => s.Status == SyncStatus.Completed && s.SyncedAt < olderThan)
            .ToListAsync();

        _context.SyncRecords.RemoveRange(oldRecords);
        await _context.SaveChangesAsync();
        
        return oldRecords.Count;
    }

    #endregion

    #region Staff Members

    public async Task<IEnumerable<StaffMember>> GetActiveStaffMembersAsync()
    {
        return await _context.StaffMembers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<StaffMember?> GetStaffByPinAsync(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin)) return null;

        return await _context.StaffMembers
            .FirstOrDefaultAsync(s => s.Pin == pin && s.IsActive);
    }

    public async Task<int> GetStaffCountAsync()
    {
        return await _context.StaffMembers
            .Where(s => s.IsActive)
            .CountAsync();
    }

    public async Task<bool> AddStaffMemberAsync(StaffMember staff)
    {
        try
        {
            staff.CreatedAt = DateTime.UtcNow;
            staff.UpdatedAt = DateTime.UtcNow;

            await _context.StaffMembers.AddAsync(staff);
            await _context.SaveChangesAsync();
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
            var existing = await _context.StaffMembers.FindAsync(staff.Id);
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

            await _context.SaveChangesAsync();
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
            var staff = await _context.StaffMembers.FindAsync(id);
            if (staff == null) return false;

            staff.IsActive = false;
            staff.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
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
        await _context.Database.EnsureCreatedAsync();
        await MigrateSchemaAsync();
    }

    /// <summary>
    /// Applies schema migrations for new columns added after initial release.
    /// SQLite doesn't support full ALTER TABLE, so we check if columns exist first.
    /// </summary>
    private async Task MigrateSchemaAsync()
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            // Get existing columns
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
            
            // Add PaymentReference column if missing
            if (!existingColumns.Contains("PaymentReference"))
            {
                Debug.WriteLine("[Migration] Adding PaymentReference column...");
                using var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Transactions ADD COLUMN PaymentReference TEXT";
                await alterCommand.ExecuteNonQueryAsync();
            }
            
            // Add CustomerPhone column if missing
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
            // Non-fatal - continue with app startup
        }
    }

    #endregion
}
