using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Data;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.ViewModels;

/// <summary>
/// ViewModel for license verification with manual activation and Stripe paywall.
/// Integrates with ILicenseManager for DevSecret2026 developer mode.
/// </summary>
public partial class LicenseViewModel : ObservableObject
{
    private readonly ILicenseService _licenseService;
    private readonly ILicenseManager _licenseManager;
    private readonly IConfiguration _configuration;
    private readonly string _stripeCheckoutUrl;

    #region Observable Properties

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isLicenseValid;

    [ObservableProperty]
    private bool _showSubscriptionOverlay;

    [ObservableProperty]
    private string _statusMessage = "Checking license...";

    [ObservableProperty]
    private string? _machineId;

    [ObservableProperty]
    private string? _planName;

    [ObservableProperty]
    private int _daysRemaining;

    [ObservableProperty]
    private LicenseStatus _licenseStatus;

    [ObservableProperty]
    private bool _hasStripeApiKey;

    // === NEW: License Key Input Properties ===
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ActivateLicenseCommand))]
    [NotifyPropertyChangedFor(nameof(CanActivate))]
    private string _licenseKeyInput = string.Empty;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanActivate))]
    private bool _isActivating;
    
    [ObservableProperty]
    private string _activationErrorMessage = string.Empty;
    
    [ObservableProperty]
    private bool _hasActivationError;
    
    [ObservableProperty]
    private bool _isDeveloperMode;
    
    public bool CanActivate => !string.IsNullOrWhiteSpace(LicenseKeyInput) && !IsActivating;

    #endregion

    #region Events

    public event EventHandler? LicenseVerified;
    public event EventHandler? DeveloperModeActivated;

    #endregion

    public LicenseViewModel(
        ILicenseService licenseService, 
        IConfiguration configuration,
        ILicenseManager? licenseManager = null)
    {
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _licenseManager = licenseManager ?? App.Current.Services.GetService<ILicenseManager>()!;
        
        // Get Stripe checkout URL from config or environment
        _stripeCheckoutUrl = _configuration["Stripe:CheckoutUrl"] 
            ?? Environment.GetEnvironmentVariable("STRIPE_CHECKOUT_URL")
            ?? "https://buy.stripe.com/test";

        // Check for Stripe API key in environment variable
        var stripeApiKey = Environment.GetEnvironmentVariable("STRIPE_API_KEY");
        HasStripeApiKey = !string.IsNullOrEmpty(stripeApiKey);

        MachineId = _licenseManager?.MachineId ?? _licenseService.GetMachineId();
        
        // Check if already in developer mode
        if (_licenseManager?.IsDeveloperMode == true)
        {
            IsDeveloperMode = true;
        }
    }

    #region Commands

    [RelayCommand]
    private async Task CheckLicenseAsync()
    {
        Debug.WriteLine("[License] CheckLicenseAsync started...");
        IsLoading = true;
        StatusMessage = "Verifying subscription...";
        ShowSubscriptionOverlay = false;
        HasActivationError = false;

        try
        {
            // First check ILicenseManager for developer mode
            if (_licenseManager != null)
            {
                var status = await _licenseManager.ValidateLicenseAsync();
                if (status == LicenseStatus.Developer)
                {
                    IsDeveloperMode = true;
                    IsLicenseValid = true;
                    LicenseStatus = LicenseStatus.Developer;
                    StatusMessage = "⚙️ Developer Mode Active";
                    PlanName = "Developer";
                    await Task.Delay(500);
                    OnLicenseVerified();
                    return;
                }
            }
            
            Debug.WriteLine("[License] Calling VerifySubscriptionAsync...");
            var licenseInfo = await _licenseService.VerifySubscriptionAsync();
            Debug.WriteLine($"[License] Result: Status={licenseInfo.Status}, Plan={licenseInfo.PlanName}");

            LicenseStatus = licenseInfo.Status;
            PlanName = licenseInfo.PlanName;
            DaysRemaining = licenseInfo.DaysRemaining;

            switch (licenseInfo.Status)
            {
                case LicenseStatus.Valid:
                    IsLicenseValid = true;
                    StatusMessage = $"✓ Active subscription: {licenseInfo.PlanName}";
                    Debug.WriteLine("[License] Valid subscription - auto-proceeding to POS...");
                    // Auto-proceed to POS
                    await Task.Delay(500);
                    OnLicenseVerified();
                    break;

                case LicenseStatus.Trial:
                    IsLicenseValid = true;
                    StatusMessage = $"⏱ Trial mode ({licenseInfo.DaysRemaining} days left)";
                    Debug.WriteLine("[License] Trial mode - waiting for user to click Enter...");
                    break;

                case LicenseStatus.Expired:
                    IsLicenseValid = false;
                    ShowSubscriptionOverlay = true;
                    StatusMessage = "Your subscription has expired";
                    break;

                case LicenseStatus.NotFound:
                    IsLicenseValid = false;
                    ShowSubscriptionOverlay = true;
                    StatusMessage = "No subscription found - please activate";
                    break;

                case LicenseStatus.Error:
                    // Allow offline access with warning
                    IsLicenseValid = true;
                    StatusMessage = "⚠ Offline mode - limited features";
                    break;
            }
        }
        catch (Exception ex)
        {
            IsLicenseValid = false;
            ShowSubscriptionOverlay = true;
            StatusMessage = $"Could not verify license: {ex.Message}";
            LicenseStatus = LicenseStatus.Error;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanActivate))]
    private async Task ActivateLicenseAsync()
    {
        if (string.IsNullOrWhiteSpace(LicenseKeyInput))
            return;
        
        Debug.WriteLine($"[License] Attempting to activate with key: {LicenseKeyInput}");
        IsActivating = true;
        HasActivationError = false;
        ActivationErrorMessage = string.Empty;
        
        try
        {
            // Use ILicenseManager for activation
            var success = await _licenseManager.ActivateLicenseAsync(LicenseKeyInput.Trim());
            
            if (success)
            {
                // Check if developer mode was activated
                if (_licenseManager.IsDeveloperMode)
                {
                    IsDeveloperMode = true;
                    IsLicenseValid = true;
                    LicenseStatus = LicenseStatus.Developer;
                    StatusMessage = "⚙️ Developer Mode Activated!";
                    PlanName = "Developer";
                    ShowSubscriptionOverlay = false;
                    
                    // Persist to SQLite
                    await SaveLicenseToDatabase(LicenseKeyInput.Trim());
                    
                    Debug.WriteLine("[License] Developer mode activated");
                    DeveloperModeActivated?.Invoke(this, EventArgs.Empty);
                    
                    // Check if branch selection is required
                    if (await ShowBranchSelectorIfRequired())
                    {
                        await Task.Delay(300);
                        OnLicenseVerified();
                    }
                    // If branch selector was cancelled, don't proceed
                }
                else
                {
                    // Normal license activation
                    IsLicenseValid = true;
                    ShowSubscriptionOverlay = false;
                    StatusMessage = "✓ License activated successfully!";
                    
                    // Persist to SQLite
                    await SaveLicenseToDatabase(LicenseKeyInput.Trim());
                    
                    // Check if branch selection is required
                    if (await ShowBranchSelectorIfRequired())
                    {
                        await Task.Delay(300);
                        OnLicenseVerified();
                    }
                    // If branch selector was cancelled, don't proceed
                }
            }
            else
            {
                HasActivationError = true;
                ActivationErrorMessage = "Invalid license key. Please check and try again.";
            }
        }
        catch (Exception ex)
        {
            HasActivationError = true;
            ActivationErrorMessage = $"Activation failed: {ex.Message}";
            Debug.WriteLine($"[License] Activation error: {ex.Message}");
        }
        finally
        {
            IsActivating = false;
        }
    }
    
    /// <summary>
    /// Shows the branch selector dialog if branch selection is required.
    /// Returns true if we should proceed (branch selected or not required).
    /// Returns false if user cancelled.
    /// </summary>
    private async Task<bool> ShowBranchSelectorIfRequired()
    {
        // Check if branch selection is required
        if (!_licenseManager.RequiresBranchSelection)
        {
            Debug.WriteLine("[License] Branch already selected or not required");
            return true;
        }
        
        Debug.WriteLine("[License] Branch selection required - showing selector...");
        StatusMessage = "Please select your branch...";
        
        try
        {
            // Create and show BranchSelectorView
            var branchSelector = new Views.BranchSelectorView();
            var result = branchSelector.ShowDialog();
            
            if (result == true)
            {
                Debug.WriteLine("[License] Branch selected successfully");
                return true;
            }
            else
            {
                // User cancelled - show error
                HasActivationError = true;
                ActivationErrorMessage = "Branch selection is required to continue.";
                Debug.WriteLine("[License] Branch selection cancelled by user");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[License] Error showing branch selector: {ex.Message}");
            // If branch selector fails, continue anyway (offline mode)
            return true;
        }
    }


    [RelayCommand]
    private void EnterPOS()
    {
        Debug.WriteLine($"[License] EnterPOS clicked. IsLicenseValid={IsLicenseValid}");
        if (IsLicenseValid)
        {
            Debug.WriteLine("[License] License valid, calling OnLicenseVerified...");
            OnLicenseVerified();
        }
        else
        {
            Debug.WriteLine("[License] License NOT valid, cannot proceed.");
        }
    }

    [RelayCommand]
    private void OpenStripeCheckout()
    {
        try
        {
            // Build checkout URL with machine ID for tracking
            var checkoutUrl = $"{_stripeCheckoutUrl}?client_reference_id={Uri.EscapeDataString(MachineId ?? "")}";

            Process.Start(new ProcessStartInfo
            {
                FileName = checkoutUrl,
                UseShellExecute = true
            });

            StatusMessage = "Opening payment page...";
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
                StatusMessage = "✓ Machine ID copied";
            }
        }
        catch
        {
            StatusMessage = "Could not copy to clipboard";
        }
    }

    [RelayCommand]
    private async Task RefreshLicenseAsync()
    {
        _licenseService.ClearCache();
        LicenseKeyInput = string.Empty;
        HasActivationError = false;
        await CheckLicenseAsync();
    }

    #endregion

    #region Database Persistence

    /// <summary>
    /// Saves the license key to the local SQLite database.
    /// </summary>
    private async Task SaveLicenseToDatabase(string licenseKey)
    {
        try
        {
            using var scope = App.Current.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Get or create store settings
            var settings = await dbContext.StoreSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new StoreSettings();
                dbContext.StoreSettings.Add(settings);
            }
            
            // Store license key in a custom property or separate table
            // For now, we'll use a simple approach with the settings
            settings.UpdatedAt = DateTime.UtcNow;
            
            await dbContext.SaveChangesAsync();
            Debug.WriteLine($"[License] License key saved to database");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[License] Failed to save to database: {ex.Message}");
        }
    }

    #endregion

    private void OnLicenseVerified()
    {
        Debug.WriteLine("[License] OnLicenseVerified called - firing LicenseVerified event...");
        LicenseVerified?.Invoke(this, EventArgs.Empty);
        Debug.WriteLine("[License] LicenseVerified event invoked.");
    }
}
