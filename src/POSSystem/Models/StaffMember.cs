using System;
using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models;

/// <summary>
/// Permission levels for staff members.
/// </summary>
public enum PermissionLevel
{
    Cashier = 0,
    Manager = 1,
    Admin = 2
}

/// <summary>
/// Represents a staff member with permissions for the POS system.
/// </summary>
public class StaffMember
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(6)]
    public string Pin { get; set; } = string.Empty; // 4-6 digit PIN

    [MaxLength(100)]
    public string? Email { get; set; }

    public PermissionLevel Level { get; set; } = PermissionLevel.Cashier;

    public bool IsActive { get; set; } = true;

    #region Permission Booleans (Degrees of Handling)

    /// <summary>
    /// Can delete or void completed transactions.
    /// </summary>
    public bool CanDeleteTransactions { get; set; } = false;

    /// <summary>
    /// Can modify product prices on the fly.
    /// </summary>
    public bool CanChangePrices { get; set; } = false;

    /// <summary>
    /// Can view sales reports and analytics.
    /// </summary>
    public bool CanViewReports { get; set; } = false;

    /// <summary>
    /// Can add, edit, or remove staff members.
    /// </summary>
    public bool CanManageStaff { get; set; } = false;

    /// <summary>
    /// Can void individual items from a transaction.
    /// </summary>
    public bool CanVoidItems { get; set; } = false;

    /// <summary>
    /// Can apply discounts to items or transactions.
    /// </summary>
    public bool CanApplyDiscounts { get; set; } = false;

    /// <summary>
    /// Can access system settings and configuration.
    /// </summary>
    public bool CanAccessSettings { get; set; } = false;

    /// <summary>
    /// Can perform end-of-day cash drawer reconciliation.
    /// </summary>
    public bool CanReconcileCashDrawer { get; set; } = false;

    #endregion

    #region Shift Tracking

    /// <summary>
    /// When the current shift started.
    /// </summary>
    public DateTime? ShiftStartTime { get; set; }

    /// <summary>
    /// Total sales amount for this employee today.
    /// </summary>
    public decimal TotalSalesToday { get; set; } = 0;

    /// <summary>
    /// Link to business for multi-tenant sync.
    /// </summary>
    public Guid? BusinessId { get; set; }

    #endregion

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Sets default permissions based on permission level.
    /// </summary>
    public void ApplyDefaultPermissions()
    {
        switch (Level)
        {
            case PermissionLevel.Cashier:
                CanDeleteTransactions = false;
                CanChangePrices = false;
                CanViewReports = false;
                CanManageStaff = false;
                CanVoidItems = false;
                CanApplyDiscounts = false;
                CanAccessSettings = false;
                CanReconcileCashDrawer = false;
                break;

            case PermissionLevel.Manager:
                CanDeleteTransactions = false;
                CanChangePrices = true;
                CanViewReports = true;
                CanManageStaff = false;
                CanVoidItems = true;
                CanApplyDiscounts = true;
                CanAccessSettings = false;
                CanReconcileCashDrawer = true;
                break;

            case PermissionLevel.Admin:
                CanDeleteTransactions = true;
                CanChangePrices = true;
                CanViewReports = true;
                CanManageStaff = true;
                CanVoidItems = true;
                CanApplyDiscounts = true;
                CanAccessSettings = true;
                CanReconcileCashDrawer = true;
                break;
        }
    }
}
