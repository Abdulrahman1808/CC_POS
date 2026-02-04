using System;
using System.Threading.Tasks;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Service for staff authentication and management.
/// </summary>
public interface IStaffService
{
    /// <summary>
    /// Attempts to authenticate a staff member by PIN.
    /// </summary>
    /// <param name="pin">The PIN to validate.</param>
    /// <returns>True if authentication succeeded.</returns>
    Task<bool> AuthenticateByPinAsync(string pin);

    /// <summary>
    /// Gets the currently logged-in staff member ID.
    /// </summary>
    Guid? CurrentStaffId { get; }

    /// <summary>
    /// Gets the currently logged-in staff member name.
    /// </summary>
    string? CurrentStaffName { get; }

    /// <summary>
    /// Whether a staff member is currently logged in.
    /// </summary>
    bool IsLoggedIn { get; }

    /// <summary>
    /// Logs out the current staff member.
    /// </summary>
    Task LogoutAsync();
}
