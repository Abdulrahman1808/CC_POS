using System;
using System.Threading.Tasks;
using POSSystem.Models;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Service for managing software licensing and developer mode.
/// </summary>
public interface ILicenseManager
{
    /// <summary>
    /// Gets the current license status.
    /// </summary>
    LicenseStatus Status { get; }
    
    /// <summary>
    /// Gets whether the application is running in developer mode.
    /// Developer mode is activated with the DevSecret2026 key.
    /// </summary>
    bool IsDeveloperMode { get; }
    
    /// <summary>
    /// Gets the current machine ID.
    /// </summary>
    string MachineId { get; }
    
    /// <summary>
    /// Gets the BusinessId associated with the activated license.
    /// Used for multi-tenant data isolation.
    /// </summary>
    Guid? BusinessId { get; }
    
    /// <summary>
    /// Activates a license key for this machine.
    /// </summary>
    /// <param name="licenseKey">The license key to activate</param>
    /// <returns>True if activation succeeded</returns>
    Task<bool> ActivateLicenseAsync(string licenseKey);
    
    /// <summary>
    /// Validates the current license.
    /// </summary>
    /// <returns>Current license status</returns>
    Task<LicenseStatus> ValidateLicenseAsync();
    
    /// <summary>
    /// Checks if a specific feature is enabled.
    /// </summary>
    /// <param name="featureName">Name of the feature</param>
    /// <returns>True if the feature is available</returns>
    bool IsFeatureEnabled(string featureName);
}
