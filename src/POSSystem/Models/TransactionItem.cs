using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace POSSystem.Models;

/// <summary>
/// Represents a line item within a transaction.
/// </summary>
public class TransactionItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid TransactionId { get; set; }
    
    [ForeignKey(nameof(TransactionId))]
    [JsonIgnore] // Prevent circular reference during JSON serialization
    public virtual Transaction? Transaction { get; set; }
    
    [Required]
    public Guid ProductId { get; set; }
    
    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    public int Quantity { get; set; }
    
    [Required]
    public decimal UnitPrice { get; set; }
    
    public decimal LineTotal => Quantity * UnitPrice;
}
