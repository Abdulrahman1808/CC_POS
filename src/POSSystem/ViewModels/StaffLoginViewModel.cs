using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSSystem.Data.Interfaces;
using POSSystem.Models;

namespace POSSystem.ViewModels;

/// <summary>
/// ViewModel for staff login with PIN entry.
/// Appears after owner license verification.
/// </summary>
public partial class StaffLoginViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly LicenseInfo _licenseInfo;

    #region Observable Properties

    [ObservableProperty]
    private string _pin = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Enter your PIN to continue";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _showStaffList = true;

    [ObservableProperty]
    private StaffMember? _selectedStaff;

    #endregion

    public ObservableCollection<StaffMember> StaffMembers { get; } = new();

    public event EventHandler<StaffLoginEventArgs>? StaffLoggedIn;

    public StaffLoginViewModel(IDataService dataService, LicenseInfo licenseInfo)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _licenseInfo = licenseInfo ?? throw new ArgumentNullException(nameof(licenseInfo));
    }

    public async Task InitializeAsync()
    {
        Debug.WriteLine("[StaffLogin] Initializing...");
        IsLoading = true;

        try
        {
            var staffList = await _dataService.GetActiveStaffMembersAsync();
            StaffMembers.Clear();

            foreach (var staff in staffList)
            {
                StaffMembers.Add(staff);
            }

            if (!StaffMembers.Any())
            {
                Debug.WriteLine("[StaffLogin] No staff found, creating default admin...");
                await CreateDefaultAdminAsync();
            }

            Debug.WriteLine($"[StaffLogin] Loaded {StaffMembers.Count} staff members");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StaffLogin] Error: {ex.Message}");
            StatusMessage = "Error loading staff";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreateDefaultAdminAsync()
    {
        var admin = new StaffMember
        {
            Name = "Admin",
            Pin = "1234",
            Level = PermissionLevel.Admin
        };
        admin.ApplyDefaultPermissions();

        await _dataService.AddStaffMemberAsync(admin);
        StaffMembers.Add(admin);

        StatusMessage = "Default admin created (PIN: 1234)";
    }

    #region Commands

    [RelayCommand]
    private void SelectStaff(StaffMember? staff)
    {
        if (staff == null) return;

        Debug.WriteLine($"[StaffLogin] Selected: {staff.Name}");
        SelectedStaff = staff;
        ShowStaffList = false;
        Pin = string.Empty;
        HasError = false;
        StatusMessage = $"Enter PIN for {staff.Name}";
    }

    [RelayCommand]
    private void BackToList()
    {
        SelectedStaff = null;
        ShowStaffList = true;
        Pin = string.Empty;
        HasError = false;
        StatusMessage = "Select your profile";
    }

    [RelayCommand]
    private void AppendDigit(string digit)
    {
        if (Pin.Length < 6)
        {
            Pin += digit;
            HasError = false;

            // Auto-submit when PIN is complete (4+ digits)
            if (Pin.Length >= 4)
            {
                _ = ValidatePinAsync();
            }
        }
    }

    [RelayCommand]
    private void ClearPin()
    {
        Pin = string.Empty;
        HasError = false;
    }

    [RelayCommand]
    private void Backspace()
    {
        if (Pin.Length > 0)
        {
            Pin = Pin[..^1];
            HasError = false;
        }
    }

    [RelayCommand]
    private async Task ValidatePinAsync()
    {
        if (SelectedStaff == null || string.IsNullOrEmpty(Pin))
            return;

        Debug.WriteLine($"[StaffLogin] Validating PIN for {SelectedStaff.Name}...");

        if (SelectedStaff.Pin == Pin)
        {
            Debug.WriteLine("[StaffLogin] PIN valid! Logging in...");
            StatusMessage = $"Welcome, {SelectedStaff.Name}!";

            // Update last login
            SelectedStaff.LastLoginAt = DateTime.UtcNow;
            await _dataService.UpdateStaffMemberAsync(SelectedStaff);

            // Fire login event
            StaffLoggedIn?.Invoke(this, new StaffLoginEventArgs(SelectedStaff));
        }
        else
        {
            Debug.WriteLine("[StaffLogin] Invalid PIN");
            HasError = true;
            StatusMessage = "Incorrect PIN";
            Pin = string.Empty;
        }
    }

    /// <summary>
    /// Adds a new staff member with plan limit check.
    /// </summary>
    [RelayCommand]
    private async Task AddStaffMemberAsync()
    {
        // Check plan limit
        var currentCount = await _dataService.GetStaffCountAsync();
        if (currentCount >= _licenseInfo.MaxEmployeeCount)
        {
            MessageBox.Show(
                $"Plan Limit Reached!\n\nYour current plan allows {_licenseInfo.MaxEmployeeCount} employees.\nUpgrade on our website to add more staff.",
                "Upgrade Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        // TODO: Show add staff dialog
        Debug.WriteLine($"[StaffLogin] Can add staff. Current: {currentCount}, Max: {_licenseInfo.MaxEmployeeCount}");
    }

    #endregion
}

public class StaffLoginEventArgs : EventArgs
{
    public StaffMember Staff { get; }

    public StaffLoginEventArgs(StaffMember staff)
    {
        Staff = staff;
    }
}
