using System;
using System.Threading.Tasks;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Event args for update available event.
/// </summary>
public class UpdateAvailableEventArgs : EventArgs
{
    public string NewVersion { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
    public long? DownloadSize { get; set; }
}

/// <summary>
/// Interface for application auto-update functionality.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Checks for available updates.
    /// </summary>
    Task<bool> CheckForUpdatesAsync();

    /// <summary>
    /// Downloads and applies the available update.
    /// </summary>
    Task ApplyUpdateAsync();

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    string GetCurrentVersion();

    /// <summary>
    /// Gets whether an update is available.
    /// </summary>
    bool IsUpdateAvailable { get; }

    /// <summary>
    /// Gets the new version if available.
    /// </summary>
    string? NewVersion { get; }

    /// <summary>
    /// Event raised when an update is available.
    /// </summary>
    event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
}
