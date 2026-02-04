using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.ViewModels;

/// <summary>
/// ViewModel for the login/license verification screen with Stripe integration.
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly ILicenseService _licenseService;
    private readonly string _stripeCheckoutUrl;

    #region Observable Properties

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLicenseValid;

    [ObservableProperty]
    private string _statusMessage = "Verifying license...";

    [ObservableProperty]
    private string? _machineId;

    [ObservableProperty]
    private string? _planName;

    [ObservableProperty]
    private int _daysRemaining;

    [ObservableProperty]
    private LicenseStatus _licenseStatus;

    [ObservableProperty]
    private bool _showPurchaseButton;

    #endregion

    #region Events

    public event EventHandler? LicenseVerified;

    #endregion

    public LoginViewModel(ILicenseService licenseService, string? stripeCheckoutUrl = null)
    {
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _stripeCheckoutUrl = stripeCheckoutUrl ?? "https://buy.stripe.com/test_your_checkout_link";
        
        MachineId = _licenseService.GetMachineId();
    }

    #region Commands

    [RelayCommand]
    private async Task VerifyLicenseAsync()
    {
        IsLoading = true;
        StatusMessage = "Verifying license...";
        ShowPurchaseButton = false;

        try
        {
            var licenseInfo = await _licenseService.VerifySubscriptionAsync();

            LicenseStatus = licenseInfo.Status;
            PlanName = licenseInfo.PlanName;
            DaysRemaining = licenseInfo.DaysRemaining;

            switch (licenseInfo.Status)
            {
                case LicenseStatus.Valid:
                    IsLicenseValid = true;
                    StatusMessage = $"✓ License valid • {licenseInfo.PlanName}";
                    OnLicenseVerified();
                    break;

                case LicenseStatus.Trial:
                    IsLicenseValid = true;
                    StatusMessage = $"⏱ Trial mode • {licenseInfo.DaysRemaining} days remaining";
                    ShowPurchaseButton = true;
                    break;

                case LicenseStatus.Expired:
                    IsLicenseValid = false;
                    StatusMessage = "⚠ Your subscription has expired";
                    ShowPurchaseButton = true;
                    break;

                case LicenseStatus.NotFound:
                    IsLicenseValid = false;
                    StatusMessage = "No license found for this machine";
                    ShowPurchaseButton = true;
                    break;

                case LicenseStatus.Error:
                    IsLicenseValid = false;
                    StatusMessage = licenseInfo.ErrorMessage ?? "Error verifying license";
                    ShowPurchaseButton = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            IsLicenseValid = false;
            StatusMessage = $"Connection error: {ex.Message}";
            LicenseStatus = LicenseStatus.Error;
            ShowPurchaseButton = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void EnterPOS()
    {
        if (IsLicenseValid)
        {
            OnLicenseVerified();
        }
    }

    [RelayCommand]
    private void PurchaseSubscription()
    {
        try
        {
            // Open Stripe checkout in browser with MachineID as client reference
            var url = $"{_stripeCheckoutUrl}?client_reference_id={Uri.EscapeDataString(MachineId ?? "")}";
            
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open browser: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyMachineId()
    {
        try
        {
            if (!string.IsNullOrEmpty(MachineId))
            {
                System.Windows.Clipboard.SetText(MachineId);
                StatusMessage = "Machine ID copied to clipboard";
            }
        }
        catch
        {
            // Clipboard access may fail
        }
    }

    #endregion

    private void OnLicenseVerified()
    {
        LicenseVerified?.Invoke(this, EventArgs.Empty);
    }
}
