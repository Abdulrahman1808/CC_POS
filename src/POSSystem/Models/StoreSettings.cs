using System;
using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models;

/// <summary>
/// Store settings for branding and receipt generation.
/// </summary>
public class StoreSettings
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(200)]
    public string StoreName { get; set; } = "My Store";

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(500)]
    public string LogoPath { get; set; } = string.Empty;

    /// <summary>
    /// Default tax rate for new products (0.14 = 14%).
    /// </summary>
    public decimal DefaultTaxRate { get; set; } = 0.14m;

    /// <summary>
    /// Currency symbol for display.
    /// </summary>
    [MaxLength(10)]
    public string CurrencySymbol { get; set; } = "EGP";

    /// <summary>
    /// Footer text for receipts.
    /// </summary>
    [MaxLength(500)]
    public string ReceiptFooter { get; set; } = "Thank you for your purchase!";

    #region Egypt ETA E-Receipt Fields

    /// <summary>
    /// Egyptian Tax Authority Tax ID (e.g., "123-456-789").
    /// Required for ETA e-receipt compliance.
    /// </summary>
    [MaxLength(50)]
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Full legal business address for receipts.
    /// Required for ETA e-receipt compliance.
    /// </summary>
    [MaxLength(500)]
    public string LegalAddress { get; set; } = string.Empty;

    #endregion

    /// <summary>
    /// Business ID for cloud sync.
    /// </summary>
    public Guid? BusinessId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
