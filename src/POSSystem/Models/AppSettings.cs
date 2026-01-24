using System;
using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models;

/// <summary>
/// Local settings stored in SQLite for offline access.
/// Stores business_id and other sync-related configuration.
/// </summary>
public class AppSettings
{
    [Key]
    public string Key { get; set; } = string.Empty;
    
    public string Value { get; set; } = string.Empty;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Business profile linking local machine to web owner.
/// Cached locally for offline operation.
/// </summary>
public class BusinessProfile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The web owner's user ID from Supabase Auth.
    /// </summary>
    public Guid? OwnerId { get; set; }
    
    /// <summary>
    /// The desktop machine's unique identifier.
    /// </summary>
    [Required]
    public string MachineId { get; set; } = string.Empty;
    
    /// <summary>
    /// Business display name.
    /// </summary>
    [MaxLength(200)]
    public string BusinessName { get; set; } = string.Empty;
    
    /// <summary>
    /// Subscription plan name (Basic, Pro, Enterprise).
    /// </summary>
    [MaxLength(50)]
    public string PlanName { get; set; } = "Basic";
    
    /// <summary>
    /// Maximum employees for current plan.
    /// </summary>
    public int MaxEmployees { get; set; } = 3;
    
    /// <summary>
    /// Whether cloud sync is enabled.
    /// </summary>
    public bool CloudSyncEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Tracks who last updated an entity for sync conflict resolution.
/// </summary>
public enum UpdateSource
{
    Desktop,
    WebDashboard,
    MobileApp,
    API
}
