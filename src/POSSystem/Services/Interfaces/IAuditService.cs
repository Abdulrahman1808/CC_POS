using System;
using System.Threading.Tasks;
using POSSystem.Models;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Service for logging auditable actions to activity_log table.
/// Automatically captures staff and tenant context.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an action to the activity log.
    /// </summary>
    /// <param name="action">The type of action performed.</param>
    /// <param name="entityType">The type of entity affected (e.g., "Transaction", "Product").</param>
    /// <param name="entityId">The ID of the affected entity.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="oldValue">Previous value (for updates).</param>
    /// <param name="newValue">New value (for updates).</param>
    Task LogActionAsync(
        AuditAction action,
        string entityType,
        Guid? entityId = null,
        string? description = null,
        string? oldValue = null,
        string? newValue = null);

    /// <summary>
    /// Logs a transaction creation.
    /// </summary>
    Task LogTransactionCreatedAsync(Guid transactionId, decimal total, string paymentMethod);

    /// <summary>
    /// Logs a transaction cancellation/void.
    /// </summary>
    Task LogTransactionCancelledAsync(Guid transactionId, string reason);

    /// <summary>
    /// Logs a price change.
    /// </summary>
    Task LogPriceChangeAsync(Guid productId, string productName, decimal oldPrice, decimal newPrice);

    /// <summary>
    /// Logs a staff login.
    /// </summary>
    Task LogStaffLoginAsync(Guid staffId, string staffName);

    /// <summary>
    /// Logs a staff logout.
    /// </summary>
    Task LogStaffLogoutAsync(Guid staffId, string staffName);

    /// <summary>
    /// Logs a discount application.
    /// </summary>
    Task LogDiscountAppliedAsync(Guid transactionId, decimal discountAmount, string reason);
}
