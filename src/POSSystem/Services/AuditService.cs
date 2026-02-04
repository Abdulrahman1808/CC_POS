using System;
using System.Diagnostics;
using System.Threading.Tasks;
using POSSystem.Data.Interfaces;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Implements audit logging to activity_log table.
/// Automatically captures tenant and staff context.
/// </summary>
public class AuditService : IAuditService
{
    private readonly IDataService _dataService;
    private readonly ITenantContext _tenantContext;

    public AuditService(IDataService dataService, ITenantContext tenantContext)
    {
        _dataService = dataService;
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public async Task LogActionAsync(
        AuditAction action,
        string entityType,
        Guid? entityId = null,
        string? description = null,
        string? oldValue = null,
        string? newValue = null)
    {
        try
        {
            var log = new ActivityLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = description ?? $"{action} on {entityType}",
                OldValue = oldValue,
                NewValue = newValue,
                StaffId = _tenantContext.CurrentStaffId,
                StaffName = _tenantContext.CurrentStaffName,
                BusinessId = _tenantContext.CurrentBusinessId,
                BranchId = _tenantContext.CurrentBranchId,
                Timestamp = DateTime.UtcNow,
                IsSynced = false
            };

            await _dataService.AddActivityLogAsync(log);
            Debug.WriteLine($"[Audit] Logged: {action} - {entityType} - {description}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Audit] Error logging action: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task LogTransactionCreatedAsync(Guid transactionId, decimal total, string paymentMethod)
    {
        await LogActionAsync(
            AuditAction.Created,
            "Transaction",
            transactionId,
            $"Transaction created: E£ {total:N2} via {paymentMethod}",
            null,
            $"Total: {total}, Payment: {paymentMethod}");
    }

    /// <inheritdoc />
    public async Task LogTransactionCancelledAsync(Guid transactionId, string reason)
    {
        await LogActionAsync(
            AuditAction.OrderCancelled,
            "Transaction",
            transactionId,
            $"Transaction cancelled: {reason}",
            null,
            reason);
    }

    /// <inheritdoc />
    public async Task LogPriceChangeAsync(Guid productId, string productName, decimal oldPrice, decimal newPrice)
    {
        await LogActionAsync(
            AuditAction.PriceChanged,
            "Product",
            productId,
            $"Price changed for {productName}: E£ {oldPrice:N2} → E£ {newPrice:N2}",
            oldPrice.ToString("N2"),
            newPrice.ToString("N2"));
    }

    /// <inheritdoc />
    public async Task LogStaffLoginAsync(Guid staffId, string staffName)
    {
        await LogActionAsync(
            AuditAction.StaffLogin,
            "StaffMember",
            staffId,
            $"Staff logged in: {staffName}");
    }

    /// <inheritdoc />
    public async Task LogStaffLogoutAsync(Guid staffId, string staffName)
    {
        await LogActionAsync(
            AuditAction.StaffLogout,
            "StaffMember",
            staffId,
            $"Staff logged out: {staffName}");
    }

    /// <inheritdoc />
    public async Task LogDiscountAppliedAsync(Guid transactionId, decimal discountAmount, string reason)
    {
        await LogActionAsync(
            AuditAction.DiscountApplied,
            "Transaction",
            transactionId,
            $"Discount applied: E£ {discountAmount:N2} - {reason}",
            null,
            $"Discount: {discountAmount}, Reason: {reason}");
    }
}
