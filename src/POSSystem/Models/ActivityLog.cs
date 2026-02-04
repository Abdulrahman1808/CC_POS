using System;
using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models;

/// <summary>
/// Types of auditable actions.
/// </summary>
public enum AuditAction
{
    Created,
    Updated,
    Deleted,
    PriceChanged,
    OrderCancelled,
    VoidItem,
    DiscountApplied,
    StaffLogin,
    StaffLogout,
    RefundIssued
}

/// <summary>
/// Represents an entry in the activity log for audit purposes.
/// Syncs to Supabase activity_log table.
/// </summary>
public class ActivityLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The action type that was performed.
    /// </summary>
    [Required]
    public AuditAction Action { get; set; }

    /// <summary>
    /// Human-readable description of the action.
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected (e.g., "Transaction", "Product", "StaffMember").
    /// </summary>
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Previous value (for updates).
    /// </summary>
    [MaxLength(1000)]
    public string? OldValue { get; set; }

    /// <summary>
    /// New value (for updates).
    /// </summary>
    [MaxLength(1000)]
    public string? NewValue { get; set; }

    /// <summary>
    /// Staff member who performed the action.
    /// </summary>
    public Guid? StaffId { get; set; }

    /// <summary>
    /// Staff member name for easier display.
    /// </summary>
    [MaxLength(100)]
    public string? StaffName { get; set; }

    /// <summary>
    /// Business ID for multi-tenant isolation.
    /// </summary>
    public Guid? BusinessId { get; set; }

    /// <summary>
    /// Branch ID for branch-level filtering.
    /// </summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// When the action occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this log has been synced to Supabase.
    /// </summary>
    public bool IsSynced { get; set; } = false;
}
