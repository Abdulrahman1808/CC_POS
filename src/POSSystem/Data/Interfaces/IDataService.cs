using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using POSSystem.Models;

namespace POSSystem.Data.Interfaces;

/// <summary>
/// Interface for data access operations following the Repository Pattern.
/// All methods are async to prevent UI blocking.
/// </summary>
public interface IDataService
{
    #region Products

    /// <summary>
    /// Gets all active products from the database.
    /// </summary>
    Task<IEnumerable<Product>> GetAllProductsAsync();

    /// <summary>
    /// Gets a product by its unique identifier.
    /// </summary>
    Task<Product?> GetProductByIdAsync(Guid id);

    /// <summary>
    /// Gets a product by its barcode.
    /// </summary>
    Task<Product?> GetProductByBarcodeAsync(string barcode);

    /// <summary>
    /// Searches products by name or barcode.
    /// </summary>
    Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);

    /// <summary>
    /// Gets products by category.
    /// </summary>
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category);

    /// <summary>
    /// Gets all distinct product categories.
    /// </summary>
    Task<IEnumerable<string>> GetCategoriesAsync();

    /// <summary>
    /// Adds a new product to the database.
    /// </summary>
    Task<bool> AddProductAsync(Product product);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    Task<bool> UpdateProductAsync(Product product);

    /// <summary>
    /// Soft-deletes a product by its ID.
    /// </summary>
    Task<bool> DeleteProductAsync(Guid id);

    #endregion

    #region Transactions

    /// <summary>
    /// Gets transactions within a date range.
    /// </summary>
    Task<IEnumerable<Transaction>> GetTransactionsAsync(DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Gets a transaction by its unique identifier.
    /// </summary>
    Task<Transaction?> GetTransactionByIdAsync(Guid id);

    /// <summary>
    /// Gets a transaction by its transaction number.
    /// </summary>
    Task<Transaction?> GetTransactionByNumberAsync(string transactionNumber);

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    Task<bool> CreateTransactionAsync(Transaction transaction);

    /// <summary>
    /// Gets today's transactions.
    /// </summary>
    Task<IEnumerable<Transaction>> GetTodayTransactionsAsync();

    /// <summary>
    /// Gets the daily sales total.
    /// </summary>
    Task<decimal> GetDailySalesTotalAsync(DateTime date);

    #endregion

    #region Sync Queue

    /// <summary>
    /// Gets all pending sync records that haven't been uploaded to the cloud.
    /// </summary>
    Task<IEnumerable<SyncRecord>> GetPendingSyncRecordsAsync();

    /// <summary>
    /// Gets the count of pending sync records.
    /// </summary>
    Task<int> GetPendingSyncCountAsync();

    /// <summary>
    /// Marks a sync record as successfully synced.
    /// </summary>
    Task<bool> MarkAsSyncedAsync(Guid syncRecordId);

    /// <summary>
    /// Marks a sync record as failed with an error message.
    /// </summary>
    Task<bool> MarkSyncFailedAsync(Guid syncRecordId, string errorMessage);

    /// <summary>
    /// Adds a record to the sync queue.
    /// </summary>
    Task<bool> AddToSyncQueueAsync(SyncRecord record);

    /// <summary>
    /// Clears all completed sync records older than the specified date.
    /// </summary>
    Task<int> ClearOldSyncRecordsAsync(DateTime olderThan);

    #endregion

    #region Staff Members

    /// <summary>
    /// Gets all active staff members.
    /// </summary>
    Task<IEnumerable<StaffMember>> GetActiveStaffMembersAsync();

    /// <summary>
    /// Gets a staff member by their PIN.
    /// </summary>
    Task<StaffMember?> GetStaffByPinAsync(string pin);

    /// <summary>
    /// Gets the total count of staff members.
    /// </summary>
    Task<int> GetStaffCountAsync();

    /// <summary>
    /// Adds a new staff member.
    /// </summary>
    Task<bool> AddStaffMemberAsync(StaffMember staff);

    /// <summary>
    /// Updates an existing staff member.
    /// </summary>
    Task<bool> UpdateStaffMemberAsync(StaffMember staff);

    /// <summary>
    /// Deactivates a staff member (soft delete).
    /// </summary>
    Task<bool> DeactivateStaffMemberAsync(Guid id);

    /// <summary>
    /// Gets all staff members (active and inactive).
    /// </summary>
    Task<IEnumerable<StaffMember>> GetStaffMembersAsync();

    #endregion

    #region Activity Log

    /// <summary>
    /// Adds an activity log entry.
    /// </summary>
    Task<bool> AddActivityLogAsync(ActivityLog log);

    /// <summary>
    /// Gets activity logs within a date range.
    /// </summary>
    Task<IEnumerable<ActivityLog>> GetActivityLogsAsync(DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Gets unsynced activity logs for cloud sync.
    /// </summary>
    Task<IEnumerable<ActivityLog>> GetUnsyncedActivityLogsAsync();

    /// <summary>
    /// Marks activity logs as synced.
    /// </summary>
    Task<bool> MarkActivityLogsSyncedAsync(IEnumerable<Guid> ids);

    #endregion

    #region Database Management

    /// <summary>
    /// Ensures the database is created and migrated.
    /// </summary>
    Task InitializeDatabaseAsync();

    /// <summary>
    /// Deletes all transactions, sync records, and resets data.
    /// </summary>
    Task<bool> ClearAllDataAsync();

    #endregion
}
