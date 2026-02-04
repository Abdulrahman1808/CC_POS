using System.Threading.Tasks;
using POSSystem.Models;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Interface for license and subscription management.
/// </summary>
public interface ILicenseService
{
    /// <summary>
    /// Gets the unique machine identifier for this device.
    /// </summary>
    string GetMachineId();

    /// <summary>
    /// Verifies the subscription status with the licensing server.
    /// </summary>
    Task<LicenseInfo> VerifySubscriptionAsync();

    /// <summary>
    /// Gets the cached license info without making a network call.
    /// </summary>
    LicenseInfo? GetCachedLicenseInfo();

    /// <summary>
    /// Clears the cached license info.
    /// </summary>
    void ClearCache();
}
