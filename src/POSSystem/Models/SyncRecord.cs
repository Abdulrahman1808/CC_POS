using System;
using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models;

/// <summary>
/// Defines the type of sync operation.
/// </summary>
public enum SyncOperation
{
    Create,
    Update,
    Delete
}

/// <summary>
/// Defines the status of a sync record.
/// </summary>
public enum SyncStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

/// <summary>
/// Represents a record in the sync queue for cloud synchronization.
/// </summary>
public class SyncRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The type of entity being synced (e.g., "Product", "Transaction").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the entity being synced.
    /// </summary>
    [Required]
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// The type of operation (Create, Update, Delete).
    /// </summary>
    [Required]
    public SyncOperation Operation { get; set; }
    
    /// <summary>
    /// JSON serialized payload of the entity data.
    /// </summary>
    public string? Payload { get; set; }
    
    /// <summary>
    /// When the sync record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the record was successfully synced (null if not yet synced).
    /// </summary>
    public DateTime? SyncedAt { get; set; }
    
    /// <summary>
    /// Current status of the sync operation.
    /// </summary>
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
    
    /// <summary>
    /// Number of retry attempts for failed syncs.
    /// </summary>
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Error message if sync failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
}
