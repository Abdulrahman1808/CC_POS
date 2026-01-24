using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using POSSystem.Models;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Interface for cloud synchronization service.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Gets whether the sync service is currently online.
    /// </summary>
    bool IsOnline { get; }

    /// <summary>
    /// Gets the number of pending sync records.
    /// </summary>
    int PendingCount { get; }

    /// <summary>
    /// Synchronizes pending records with the cloud.
    /// </summary>
    Task<bool> SyncAsync();

    /// <summary>
    /// Checks the connection to the cloud service.
    /// </summary>
    Task<bool> CheckConnectionAsync();

    /// <summary>
    /// Event raised when sync status changes.
    /// </summary>
    event EventHandler<SyncStatusChangedEventArgs>? SyncStatusChanged;
}

/// <summary>
/// Event args for sync status changes.
/// </summary>
public class SyncStatusChangedEventArgs : EventArgs
{
    public bool IsOnline { get; set; }
    public int PendingCount { get; set; }
    public string? Message { get; set; }
}
