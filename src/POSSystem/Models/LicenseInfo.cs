namespace POSSystem.Models;

/// <summary>
/// Represents the subscription license status.
/// </summary>
public enum LicenseStatus
{
    /// <summary>
    /// License is valid and active.
    /// </summary>
    Valid,
    
    /// <summary>
    /// License has expired.
    /// </summary>
    Expired,
    
    /// <summary>
    /// No license found for this machine.
    /// </summary>
    NotFound,
    
    /// <summary>
    /// License is in trial period.
    /// </summary>
    Trial,
    
    /// <summary>
    /// Developer mode - all features unlocked for testing.
    /// Activated with DevSecret2026 key.
    /// </summary>
    Developer,
    
    /// <summary>
    /// Error occurred while verifying license.
    /// </summary>
    Error
}

/// <summary>
/// Contains license information.
/// </summary>
public class LicenseInfo
{
    public LicenseStatus Status { get; set; }
    public string? MachineId { get; set; }
    public string? SubscriptionId { get; set; }
    public string? PlanName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Maximum number of employees allowed by the current plan.
    /// </summary>
    public int MaxEmployeeCount { get; set; } = 3; // Default for Basic plan
    
    /// <summary>
    /// Maximum number of products allowed by the current plan.
    /// </summary>
    public int MaxProductCount { get; set; } = 100; // Default for Basic plan
    
    /// <summary>
    /// Whether cloud sync is enabled for this plan.
    /// </summary>
    public bool CloudSyncEnabled { get; set; } = true;
    
    public int DaysRemaining => ExpiresAt.HasValue 
        ? Math.Max(0, (ExpiresAt.Value - DateTime.UtcNow).Days) 
        : 0;
}
