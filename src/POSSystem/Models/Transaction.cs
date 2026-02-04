using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models;

/// <summary>
/// Represents a sales transaction in the POS system.
/// </summary>
public class Transaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string TransactionNumber { get; set; } = string.Empty;
    
    public virtual ICollection<TransactionItem> Items { get; set; } = new List<TransactionItem>();
    
    public decimal SubTotal { get; set; }
    
    public decimal TaxRate { get; set; } = 0.14m; // 14% default tax
    
    public decimal TaxAmount { get; set; }
    
    public decimal Total { get; set; }
    
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash";
    
    /// <summary>
    /// Fawry reference number or card authorization code.
    /// </summary>
    [MaxLength(50)]
    public string? PaymentReference { get; set; }
    
    public Guid? CustomerId { get; set; }
    
    [MaxLength(200)]
    public string? CustomerName { get; set; }
    
    /// <summary>
    /// Customer phone number for Mobile/Card receipts.
    /// </summary>
    [MaxLength(20)]
    public string? CustomerPhone { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsSynced { get; set; } = false;
    
    #region Sync Fields
    
    /// <summary>
    /// Links transaction to business profile.
    /// </summary>
    public Guid? BusinessId { get; set; }
    
    /// <summary>
    /// Links transaction to specific branch.
    /// </summary>
    public Guid? BranchId { get; set; }
    
    /// <summary>
    /// Staff member who processed this transaction.
    /// </summary>
    public Guid? StaffMemberId { get; set; }
    
    /// <summary>
    /// Tracks update source for sync conflict resolution.
    /// </summary>
    public UpdateSource LastUpdatedBy { get; set; } = UpdateSource.Desktop;
    
    #endregion

    
    /// <summary>
    /// Calculates totals based on items.
    /// </summary>
    public void CalculateTotals()
    {
        SubTotal = 0;
        foreach (var item in Items)
        {
            SubTotal += item.LineTotal;
        }
        TaxAmount = SubTotal * TaxRate;
        Total = SubTotal + TaxAmount;
    }
}
