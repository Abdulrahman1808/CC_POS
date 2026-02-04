using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSSystem.Data.Interfaces;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.ViewModels;

public partial class RefundViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IThermalReceiptService _receiptService;
    private readonly ISyncService _syncService;
    private readonly IAuditService _auditService;
    private readonly ITenantContext _tenantContext;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Transaction> _searchResults = new();

    [ObservableProperty]
    private Transaction? _selectedTransaction;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public RefundViewModel(
        IDataService dataService,
        IThermalReceiptService receiptService,
        ISyncService syncService,
        IAuditService auditService,
        ITenantContext tenantContext)
    {
        _dataService = dataService;
        _receiptService = receiptService;
        _syncService = syncService;
        _auditService = auditService;
        _tenantContext = tenantContext;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            StatusMessage = "Please enter a transaction ID or phone number";
            return;
        }

        IsProcessing = true;
        StatusMessage = "Searching...";
        SearchResults.Clear();

        try
        {
            // Search locally first
            // Note: Since IDataService doesn't have SearchTransactions, we fetch generic recent ones or by ID
            // Ideally IDataService should be extended with SearchTransactionsAsync
            
            // For now, try fetching by ID directly if it looks like a GUID
            if (Guid.TryParse(SearchQuery, out var id))
            {
                var tx = await _dataService.GetTransactionByIdAsync(id);
                if (tx != null) SearchResults.Add(tx);
            }
            else
            {
                // Basic number search or phone search
                // Requires extending IDataService, but for now we can try exact number match
                var tx = await _dataService.GetTransactionByNumberAsync(SearchQuery);
                if (tx != null) SearchResults.Add(tx);
            }

            if (SearchResults.Count == 0)
            {
                StatusMessage = "No transaction found.";
            }
            else
            {
                StatusMessage = $"Found {SearchResults.Count} transaction(s).";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ProcessRefundAsync(Transaction transaction)
    {
        if (transaction == null) return;
        if (transaction.Status == TransactionStatus.Refunded)
        {
            StatusMessage = "This transaction is already refunded.";
            return;
        }

        IsProcessing = true;
        StatusMessage = "Processing refund...";

        try
        {
            // 1. Update Transaction Status
            transaction.Status = TransactionStatus.Refunded;
            await _dataService.UpdateTransactionAsync(transaction);
            
            // 2. Restore Stock
            foreach (var item in transaction.Items)
            {
                var product = await _dataService.GetProductByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.RetailQuantity += item.Quantity; // Add back to retail stock
                    await _dataService.UpdateProductAsync(product);
                }
            }

            // 3. Sync Update (Queue a PATCH)
            var syncRecord = new SyncRecord
            {
                EntityId = transaction.Id,
                EntityType = "Transaction",
                Operation = SyncOperation.Update,
                Payload = System.Text.Json.JsonSerializer.Serialize(new { status = "Refunded" }),
                CreatedAt = DateTime.UtcNow,
                Status = SyncStatus.Pending
            };
            await _dataService.AddToSyncQueueAsync(syncRecord);
            
            // 4. Audit Log
            await _auditService.LogActionAsync(
                AuditAction.RefundIssued,
                "Transaction",
                transaction.Id,
                $"Refund issued for {transaction.TransactionNumber}",
                "Completed",
                "Refunded"
            );

            // 5. Print Refund Receipt
            var receipt = new ReceiptModel
            {
                StoreName = "REFUND RECEIPT",
                TransactionNumber = transaction.TransactionNumber + "-R",
                BranchName = _tenantContext.CurrentBranchName,
                Timestamp = DateTime.UtcNow,
                ReceiptFooter = "Refund Issued"
            };
            // Add items with negative qty? Or just list them.
            receipt.Items = transaction.Items.Select(i => new ReceiptLineItem
            {
                ProductName = i.ProductName,
                Quantity = -i.Quantity,
                UnitPrice = i.UnitPrice,
            }).ToList();

            await _receiptService.PrintReceiptAsync(receipt);

            StatusMessage = "Refund processed successfully.";
            
            // Refresh results
            await SearchAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Refund failed: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
