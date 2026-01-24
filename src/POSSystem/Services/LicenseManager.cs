using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Manages software licensing and developer mode activation.
/// Developer mode is activated with the DevSecret2026 key.
/// </summary>
public class LicenseManager : ILicenseManager
{
    private const string DeveloperSecretKey = "DevSecret2026";
    private const string LicenseFileName = ".license";
    
    private readonly IHardwareIdService _hardwareIdService;
    private readonly IConfiguration _configuration;
    private readonly string _licensePath;
    
    private LicenseStatus _status = LicenseStatus.NotFound;
    private bool _isDeveloperMode = false;
    private string? _activeLicenseKey;
    
    public LicenseManager(IHardwareIdService hardwareIdService, IConfiguration configuration)
    {
        _hardwareIdService = hardwareIdService ?? throw new ArgumentNullException(nameof(hardwareIdService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        _licensePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "POSSystem",
            LicenseFileName);
        
        // Auto-validate on construction
        _ = ValidateLicenseAsync();
    }
    
    /// <inheritdoc />
    public LicenseStatus Status => _status;
    
    /// <inheritdoc />
    public bool IsDeveloperMode => _isDeveloperMode;
    
    /// <inheritdoc />
    public string MachineId => _hardwareIdService.GetMachineId();
    
    /// <inheritdoc />
    public async Task<bool> ActivateLicenseAsync(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return false;
        
        try
        {
            // Check for developer mode activation
            if (licenseKey == DeveloperSecretKey)
            {
                _isDeveloperMode = true;
                _status = LicenseStatus.Developer;
                _activeLicenseKey = licenseKey;
                
                // Save developer key
                await SaveLicenseKeyAsync(licenseKey);
                
                Debug.WriteLine("[LicenseManager] ⚠️ DEVELOPER MODE ACTIVATED");
                return true;
            }
            
            // Normal license validation
            var isValid = await ValidateLicenseKeyAsync(licenseKey);
            
            if (isValid)
            {
                _status = LicenseStatus.Valid;
                _activeLicenseKey = licenseKey;
                _isDeveloperMode = false;
                
                await SaveLicenseKeyAsync(licenseKey);
                Debug.WriteLine("[LicenseManager] License activated successfully");
                return true;
            }
            
            Debug.WriteLine("[LicenseManager] License key invalid");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LicenseManager] Activation error: {ex.Message}");
            _status = LicenseStatus.Error;
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<LicenseStatus> ValidateLicenseAsync()
    {
        try
        {
            // Load stored license key
            var storedKey = await LoadLicenseKeyAsync();
            
            if (string.IsNullOrEmpty(storedKey))
            {
                // Check if in DEBUG mode - auto-enable trial
#if DEBUG
                _status = LicenseStatus.Trial;
                Debug.WriteLine("[LicenseManager] DEBUG build - Trial mode enabled");
#else
                _status = LicenseStatus.NotFound;
#endif
                return _status;
            }
            
            // Check for developer mode
            if (storedKey == DeveloperSecretKey)
            {
                _isDeveloperMode = true;
                _status = LicenseStatus.Developer;
                _activeLicenseKey = storedKey;
                Debug.WriteLine("[LicenseManager] ⚠️ DEVELOPER MODE ACTIVE");
                return _status;
            }
            
            // Validate stored key
            var isValid = await ValidateLicenseKeyAsync(storedKey);
            _status = isValid ? LicenseStatus.Valid : LicenseStatus.Expired;
            _activeLicenseKey = storedKey;
            
            return _status;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LicenseManager] Validation error: {ex.Message}");
            _status = LicenseStatus.Error;
            return _status;
        }
    }
    
    /// <inheritdoc />
    public bool IsFeatureEnabled(string featureName)
    {
        // Developer mode unlocks all features
        if (_isDeveloperMode)
            return true;
        
        // Valid license unlocks all features
        if (_status == LicenseStatus.Valid)
            return true;
        
        // Trial mode - limited features
        if (_status == LicenseStatus.Trial)
        {
            // Allow basic features in trial
            return featureName switch
            {
                "BasicPOS" => true,
                "Receipts" => true,
                "Inventory" => true,
                "Reports" => false,      // Premium
                "CloudSync" => false,    // Premium
                "MultiStore" => false,   // Enterprise
                _ => false
            };
        }
        
        return false;
    }
    
    /// <summary>
    /// Validates a license key against the licensing server or local rules.
    /// </summary>
    private async Task<bool> ValidateLicenseKeyAsync(string licenseKey)
    {
        // TODO: Implement actual license server validation
        // For now, use local validation logic
        
        await Task.Delay(100); // Simulate network call
        
        // Basic validation: Must be 20+ characters and contain machine ID parts
        if (licenseKey.Length < 20)
            return false;
        
        // Check if key contains a valid signature pattern
        // Format: POSKEY-{hash}-{machinePrefix}
        if (licenseKey.StartsWith("POSKEY-"))
        {
            var machineId = _hardwareIdService.GetMachineId();
            var machinePrefix = machineId.Substring(0, 8);
            
            // Key should end with machine prefix for device binding
            return licenseKey.EndsWith(machinePrefix);
        }
        
        return false;
    }
    
    /// <summary>
    /// Saves the license key to local storage.
    /// </summary>
    private async Task SaveLicenseKeyAsync(string licenseKey)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_licensePath)!);
            await File.WriteAllTextAsync(_licensePath, licenseKey);
            Debug.WriteLine($"[LicenseManager] License saved to {_licensePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LicenseManager] Failed to save license: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Loads the license key from local storage.
    /// </summary>
    private async Task<string?> LoadLicenseKeyAsync()
    {
        try
        {
            if (File.Exists(_licensePath))
            {
                return (await File.ReadAllTextAsync(_licensePath)).Trim();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LicenseManager] Failed to load license: {ex.Message}");
        }
        
        return null;
    }
}
