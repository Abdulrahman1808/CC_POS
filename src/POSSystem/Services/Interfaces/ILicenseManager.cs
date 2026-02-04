using System;
using System.Threading.Tasks;
using POSSystem.Models;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Service for managing software licensing and developer mode.
/// Supports hardware locking to specific branches.
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
    /// Gets the BranchId that this machine is locked to.
    /// Null if no branch is bound yet.
    /// </summary>
    Guid? BranchId { get; }
    
    /// <summary>
    /// Gets whether a branch has been bound to this machine.
    /// </summary>
    bool IsBranchBound { get; }
    
    /// <summary>
    /// Gets whether the license requires branch selection before proceeding.
    /// Returns true if license is valid but no branch is bound.
    /// </summary>
    bool RequiresBranchSelection { get; }
    
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
    
    /// <summary>
    /// Binds this machine to a specific branch.
    /// This is a one-time operation - the machine cannot be unbound without re-activation.
    /// </summary>
    /// <param name="branchId">The branch to bind this machine to</param>
    /// <param name="branchName">Display name for logging</param>
    /// <returns>True if binding succeeded</returns>
    Task<bool> BindToBranchAsync(Guid branchId, string branchName);
}
