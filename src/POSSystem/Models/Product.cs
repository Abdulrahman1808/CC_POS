using System;
using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models;

/// <summary>
/// Represents a product in the POS system inventory.
/// </summary>
public class Product
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public decimal Price { get; set; }
    
    [MaxLength(50)]
    public string? Barcode { get; set; }
    
    [MaxLength(50)]
    public string? Sku { get; set; }
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    public int StockQuantity { get; set; }
    
    /// <summary>
    /// Cost price for profit calculations.
    /// </summary>
    public decimal Cost { get; set; } = 0;
    
    /// <summary>
    /// Product-specific tax rate (0.14 = 14%).
    /// </summary>
    public decimal TaxRate { get; set; } = 0.14m;
    
    /// <summary>
    /// Minimum stock level before warning.
    /// </summary>
    public int MinStockLevel { get; set; } = 5;
    
    /// <summary>
    /// Quantity to reorder when low.
    /// </summary>
    public int ReorderPoint { get; set; } = 10;
    
    [MaxLength(500)]
    public string? ImagePath { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// UI-only: For checkbox multi-select (not persisted).
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool IsSelected { get; set; } = false;
    
    /// <summary>
    /// UI-only: Indicates if stock is at or below MinStockLevel.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool IsLowStock => StockQuantity <= MinStockLevel;
    
    #region Sync Fields
    
    /// <summary>
    /// Links product to business profile for multi-tenant sync.
    /// </summary>
    public Guid? BusinessId { get; set; }
    
    /// <summary>
    /// Links product to specific branch for multi-branch isolation.
    /// Null means product is shared across all branches.
    /// </summary>
    public Guid? BranchId { get; set; }
    
    /// <summary>
    /// Tracks who last updated this record for conflict resolution.
    /// </summary>
    public UpdateSource LastUpdatedBy { get; set; } = UpdateSource.Desktop;
    
    #endregion

    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsDeleted { get; set; } = false;
}
