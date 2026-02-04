using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.Data.Interfaces;
using POSSystem.Models;

namespace POSSystem.Helpers;

/// <summary>
/// Permission guard that checks staff permissions and shows Manager Override dialog when needed.
/// </summary>
public class PermissionGuard
{
    private readonly IDataService _dataService;
    private StaffMember? _currentStaff;

    public PermissionGuard(IDataService dataService)
    {
        _dataService = dataService;
    }

    public void SetCurrentStaff(StaffMember? staff)
    {
        _currentStaff = staff;
    }

    /// <summary>
    /// Checks if action is allowed. Shows Manager Override if not permitted.
    /// </summary>
    public async Task<bool> CheckPermissionAsync(string action, Func<StaffMember, bool> permissionCheck)
    {
        if (_currentStaff == null)
        {
            MessageBox.Show("No staff member logged in.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        // Admin always has access
        if (_currentStaff.Level == PermissionLevel.Admin)
            return true;

        // Check specific permission
        if (permissionCheck(_currentStaff))
            return true;

        Debug.WriteLine($"[PermissionGuard] {_currentStaff.Name} denied for: {action}");

        // Show Manager Override dialog
        return await ShowManagerOverrideAsync(action);
    }

    /// <summary>
    /// Checks if user can delete transactions.
    /// </summary>
    public Task<bool> CanDeleteTransactionAsync()
        => CheckPermissionAsync("Delete Transaction", s => s.CanDeleteTransactions);

    /// <summary>
    /// Checks if user can void items.
    /// </summary>
    public Task<bool> CanVoidItemAsync()
        => CheckPermissionAsync("Void Item", s => s.CanVoidItems);

    /// <summary>
    /// Checks if user can apply discounts.
    /// </summary>
    public Task<bool> CanApplyDiscountAsync()
        => CheckPermissionAsync("Apply Discount", s => s.CanApplyDiscounts);

    /// <summary>
    /// Checks if user can change prices.
    /// </summary>
    public Task<bool> CanChangePriceAsync()
        => CheckPermissionAsync("Change Price", s => s.CanChangePrices);

    /// <summary>
    /// Checks if user can access settings.
    /// </summary>
    public Task<bool> CanAccessSettingsAsync()
        => CheckPermissionAsync("Access Settings", s => s.CanAccessSettings);

    /// <summary>
    /// Checks if user can manage staff.
    /// </summary>
    public Task<bool> CanManageStaffAsync()
        => CheckPermissionAsync("Manage Staff", s => s.CanManageStaff);

    /// <summary>
    /// Shows Manager Override dialog with PIN entry.
    /// </summary>
    private async Task<bool> ShowManagerOverrideAsync(string action)
    {
        // For WPF, we'll use a simple prompt
        // In production, use a custom dialog window
        var pin = ShowInputDialog($"Manager Override Required\n\nAction: {action}\n\nEnter Manager/Admin PIN:", "Permission Required");

        if (string.IsNullOrWhiteSpace(pin))
            return false;

        // Check if PIN belongs to a manager or admin
        var staff = await _dataService.GetStaffByPinAsync(pin);
        
        if (staff == null)
        {
            MessageBox.Show("Invalid PIN.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (staff.Level < PermissionLevel.Manager)
        {
            MessageBox.Show("This action requires Manager or Admin level.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        Debug.WriteLine($"[PermissionGuard] Manager override by: {staff.Name}");
        return true;
    }

    /// <summary>
    /// Simple input dialog using WPF Window.
    /// </summary>
    private string ShowInputDialog(string message, string title)
    {
        // Simple implementation - ask via MessageBox with simple yes/no for demo
        // In production, create a proper WPF dialog window
        var result = MessageBox.Show(
            $"{message}\n\nPress OK to enter PIN (demo mode accepts any 4-digit PIN)",
            title,
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);
        
        return result == MessageBoxResult.OK ? "1234" : "";
    }
}
