using System;
using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models;

/// <summary>
/// Represents a branch/location within a business.
/// Used for multi-branch POS deployments.
/// </summary>
public class Branch
{
    /// <summary>
    /// Unique identifier for the branch.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The business this branch belongs to.
    /// </summary>
    [Required]
    public Guid BusinessId { get; set; }
    
    /// <summary>
    /// Display name for the branch.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Arabic name for RTL display.
    /// </summary>
    [MaxLength(100)]
    public string? NameAr { get; set; }
    
    /// <summary>
    /// Physical address of the branch.
    /// </summary>
    [MaxLength(500)]
    public string? Address { get; set; }
    
    /// <summary>
    /// Arabic address for RTL display.
    /// </summary>
    [MaxLength(500)]
    public string? AddressAr { get; set; }
    
    /// <summary>
    /// Branch phone number.
    /// </summary>
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    /// <summary>
    /// Whether this branch is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When the branch record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets the localized display name (Arabic if available, else English).
    /// </summary>
    public string DisplayName => !string.IsNullOrEmpty(NameAr) ? NameAr : Name;
    
    /// <summary>
    /// Gets the localized address.
    /// </summary>
    public string? DisplayAddress => !string.IsNullOrEmpty(AddressAr) ? AddressAr : Address;
}
