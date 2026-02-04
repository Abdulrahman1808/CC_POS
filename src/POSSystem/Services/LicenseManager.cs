using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Manages software licensing, developer mode, and multi-tenant business context.
/// License keys contain embedded BusinessId for tenant isolation.
/// Developer mode is activated with the DevSecret2026 key.
/// </summary>
public class LicenseManager : ILicenseManager
{
    private const string DeveloperSecretKey = "DevSecret2026";
    private const string LicenseFileName = ".license";
    
    // Developer mode uses a fixed BusinessId for testing
    private static readonly Guid DeveloperBusinessId = new("de000000-0000-0000-0000-000000000001");
    
    private readonly IHardwareIdService _hardwareIdService;
    private readonly ITenantContext _tenantContext;
    private readonly IConfiguration _configuration;
    private readonly string _licensePath;
    
    private LicenseStatus _status = LicenseStatus.NotFound;
    private bool _isDeveloperMode = false;
    private string? _activeLicenseKey;
    
    public LicenseManager(
        IHardwareIdService hardwareIdService, 
        ITenantContext tenantContext,
        IConfiguration configuration)
    {
        _hardwareIdService = hardwareIdService ?? throw new ArgumentNullException(nameof(hardwareIdService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
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
    public Guid? BusinessId => _tenantContext.CurrentBusinessId;
    
    /// <inheritdoc />
    public Guid? BranchId => _tenantContext.CurrentBranchId;
    
    /// <inheritdoc />
    public bool IsBranchBound => _tenantContext.IsBranchSelected;
    
    /// <inheritdoc />
    public bool RequiresBranchSelection => _status == LicenseStatus.Valid && !_tenantContext.IsBranchSelected;
    
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
                
                // Set developer BusinessId in tenant context
                _tenantContext.SetBusinessContext(DeveloperBusinessId);
                
                // Save developer key
                await SaveLicenseKeyAsync(licenseKey);
                
                Debug.WriteLine($"[LicenseManager] ⚠️ DEVELOPER MODE ACTIVATED - BusinessId: {DeveloperBusinessId}");
                return true;
            }
            
            // Normal license validation with BusinessId extraction
            var (isValid, businessId) = await ValidateLicenseKeyWithBusinessIdAsync(licenseKey);
            
            if (isValid && businessId.HasValue)
            {
                _status = LicenseStatus.Valid;
                _activeLicenseKey = licenseKey;
                _isDeveloperMode = false;
                
                // Set the BusinessId in tenant context
                _tenantContext.SetBusinessContext(businessId.Value);
                
                await SaveLicenseKeyAsync(licenseKey);
                Debug.WriteLine($"[LicenseManager] License activated - BusinessId: {businessId.Value}");
                return true;
            }
            
            Debug.WriteLine("[LicenseManager] License key invalid or missing BusinessId");
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
                // Check if in DEBUG mode - auto-enable trial with developer BusinessId
#if DEBUG
                _status = LicenseStatus.Trial;
                _tenantContext.SetBusinessContext(DeveloperBusinessId);
                Debug.WriteLine("[LicenseManager] DEBUG build - Trial mode enabled with Developer BusinessId");
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
                
                // Ensure tenant context is set for developer mode
                if (!_tenantContext.IsContextValid)
                {
                    _tenantContext.SetBusinessContext(DeveloperBusinessId);
                }
                
                Debug.WriteLine($"[LicenseManager] ⚠️ DEVELOPER MODE ACTIVE - BusinessId: {_tenantContext.CurrentBusinessId}");
                return _status;
            }
            
            // Validate stored key and extract BusinessId
            var (isValid, businessId) = await ValidateLicenseKeyWithBusinessIdAsync(storedKey);
            _status = isValid ? LicenseStatus.Valid : LicenseStatus.Expired;
            _activeLicenseKey = storedKey;
            
            // Restore tenant context if valid
            if (isValid && businessId.HasValue && !_tenantContext.IsContextValid)
            {
                _tenantContext.SetBusinessContext(businessId.Value);
            }
            
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
    /// Validates a license key and extracts the embedded BusinessId.
    /// License key format: POSKEY-{BusinessIdPrefix}-{Hash}-{MachinePrefix}
    /// Example: POSKEY-A1B2C3D4-randomhash123-12345678
    /// </summary>
    private async Task<(bool IsValid, Guid? BusinessId)> ValidateLicenseKeyWithBusinessIdAsync(string licenseKey)
    {
        // TODO: Implement actual license server validation
        // For now, use local validation logic with BusinessId extraction
        
        await Task.Delay(100); // Simulate network call
        
        // Basic validation: Must be 20+ characters
        if (licenseKey.Length < 20)
            return (false, null);
        
        // Check if key contains a valid signature pattern
        // Format: POSKEY-{BusinessIdPrefix}-{Hash}-{MachinePrefix}
        if (licenseKey.StartsWith("POSKEY-"))
        {
            var parts = licenseKey.Split('-');
            
            // Expect at least 4 parts: POSKEY, BusinessIdPrefix, Hash, MachinePrefix
            if (parts.Length >= 4)
            {
                var businessIdPrefix = parts[1];
                var machinePrefix = parts[^1]; // Last part
                
                // Validate machine binding
                var machineId = _hardwareIdService.GetMachineId();
                var expectedMachinePrefix = machineId.Substring(0, Math.Min(8, machineId.Length));
                
                if (machinePrefix != expectedMachinePrefix)
                {
                    Debug.WriteLine($"[LicenseManager] Machine mismatch: expected {expectedMachinePrefix}, got {machinePrefix}");
                    return (false, null);
                }
                
                // Generate deterministic BusinessId from prefix
                // This creates a consistent GUID from the 8-character prefix
                var businessId = GenerateBusinessIdFromPrefix(businessIdPrefix);
                
                return (true, businessId);
            }
        }
        
        return (false, null);
    }
    
    /// <summary>
    /// Generates a deterministic BusinessId GUID from a license key prefix.
    /// </summary>
    private static Guid GenerateBusinessIdFromPrefix(string prefix)
    {
        // Pad or truncate to 8 characters, then create a deterministic GUID
        var normalizedPrefix = (prefix + "00000000").Substring(0, 8);
        
        // Create GUID in format: PREFIX00-0000-0000-0000-000000000000
        return new Guid($"{normalizedPrefix}-0000-0000-0000-000000000000");
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
    
    /// <inheritdoc />
    public async Task<bool> BindToBranchAsync(Guid branchId, string branchName)
    {
        if (branchId == Guid.Empty)
            return false;
            
        if (string.IsNullOrWhiteSpace(branchName))
            return false;
            
        try
        {
            // Validate we have a valid license first
            if (_status != LicenseStatus.Valid && _status != LicenseStatus.Developer)
            {
                Debug.WriteLine("[LicenseManager] Cannot bind branch - no valid license");
                return false;
            }
            
            // Check if already bound to a different branch
            if (_tenantContext.IsBranchSelected && _tenantContext.CurrentBranchId != branchId)
            {
                Debug.WriteLine($"[LicenseManager] ⚠️ Machine already bound to branch {_tenantContext.CurrentBranchId}");
                return false;
            }
            
            // Set branch context (this persists with DPAPI encryption)
            _tenantContext.SetBranchContext(branchId, branchName);
            
            Debug.WriteLine($"[LicenseManager] 🔒 Machine bound to branch: {branchName} ({branchId})");
            Debug.WriteLine($"[LicenseManager] Hardware ID: {MachineId}");
            
            // Trigger a save to update the license file metadata
            if (!string.IsNullOrEmpty(_activeLicenseKey))
            {
                await SaveLicenseKeyAsync(_activeLicenseKey);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LicenseManager] Failed to bind branch: {ex.Message}");
            return false;
        }
    }
}
