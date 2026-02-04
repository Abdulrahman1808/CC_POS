using System;
using System.Collections.Generic;
using System.Linq;

namespace POSSystem.Models;

/// <summary>
/// Unified data contract for all document generation (Thermal Receipts &amp; PDF Invoices).
/// Provides centralized VAT calculations to ensure consistency across UI, receipts, and invoices.
/// </summary>
public class ReceiptModel
{
    #region Store Information
    
    /// <summary>
    /// Store/Business name to display on documents.
    /// </summary>
    /// <remarks>
    /// TODO: Arabic localization - Apply Arabic shaping/reshaping here for RTL display.
    /// Consider using a library like ArabicReshaper for proper glyph joining.
    /// </remarks>
    public string StoreName { get; set; } = string.Empty;
    
    /// <summary>
    /// Full store address.
    /// </summary>
    public string StoreAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Store phone number.
    /// </summary>
    public string StorePhone { get; set; } = string.Empty;
    
    /// <summary>
    /// Store email address.
    /// </summary>
    public string StoreEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Egyptian Tax Authority Tax Registration Number.
    /// Required for ETA e-receipt compliance.
    /// </summary>
    public string TaxRegistrationNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to the store logo image file.
    /// </summary>
    public string LogoPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Currency symbol for display (default: EGP).
    /// </summary>
    public string CurrencySymbol { get; set; } = "EGP";
    
    /// <summary>
    /// Footer message for receipts.
    /// </summary>
    public string ReceiptFooter { get; set; } = "Thank you for your purchase!";
    
    /// <summary>
    /// Branch ID for multi-branch businesses.
    /// </summary>
    public Guid? BranchId { get; set; }
    
    /// <summary>
    /// Branch name for display on receipts.
    /// </summary>
    public string? BranchName { get; set; }
    
    #endregion


    #region Transaction Information
    
    /// <summary>
    /// Unique transaction/receipt number.
    /// </summary>
    public string TransactionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Value for the Code128 barcode (usually same as TransactionNumber).
    /// </summary>
    public string BarcodeValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Payment method used (Cash, Card, Mobile).
    /// </summary>
    public string PaymentMethod { get; set; } = "Cash";
    
    /// <summary>
    /// Payment reference (e.g., Fawry reference code FWY-XXXX-XXXX).
    /// </summary>
    public string? PaymentReference { get; set; }
    
    /// <summary>
    /// Customer name (if provided).
    /// </summary>
    public string? CustomerName { get; set; }
    
    /// <summary>
    /// Customer phone number (for e-receipts).
    /// </summary>
    public string? CustomerPhone { get; set; }
    
    /// <summary>
    /// Staff member who processed the transaction.
    /// </summary>
    public string? CashierName { get; set; }
    
    #endregion

    #region Line Items
    
    /// <summary>
    /// List of items in the transaction.
    /// </summary>
    public List<ReceiptLineItem> Items { get; set; } = new();
    
    #endregion

    #region VAT Calculations (Centralized - Single Source of Truth)
    
    /// <summary>
    /// VAT rate as decimal (0.14 = 14%).
    /// Egyptian standard VAT is 14%.
    /// </summary>
    public decimal VatRate { get; set; } = 0.14m;
    
    /// <summary>
    /// Subtotal before VAT - calculated from line items.
    /// </summary>
    public decimal SubTotal => Items.Sum(i => i.LineTotal);
    
    /// <summary>
    /// VAT amount calculated from subtotal.
    /// </summary>
    public decimal VatAmount => Math.Round(SubTotal * VatRate, 2);
    
    /// <summary>
    /// Grand total including VAT.
    /// </summary>
    public decimal Total => SubTotal + VatAmount;
    
    /// <summary>
    /// VAT rate as percentage for display (e.g., "14%").
    /// </summary>
    public string VatRateDisplay => $"{VatRate * 100:0}%";
    
    #endregion

    #region ETA Compliance
    
    /// <summary>
    /// Base64-encoded TLV string for ETA-compliant QR code.
    /// Generated using EtaQrCodeHelper.GenerateTlvBase64().
    /// </summary>
    public string EtaTlvBase64 { get; set; } = string.Empty;
    
    #endregion
}

/// <summary>
/// Represents a single line item in the receipt.
/// </summary>
public class ReceiptLineItem
{
    /// <summary>
    /// Product ID reference.
    /// </summary>
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// Display name of the product.
    /// </summary>
    /// <remarks>
    /// TODO: Arabic localization - Apply Arabic shaping here for RTL display.
    /// </remarks>
    public string ProductName { get; set; } = string.Empty;
    
    /// <summary>
    /// Quantity purchased.
    /// </summary>
    public int Quantity { get; set; }
    
    /// <summary>
    /// Unit price before tax.
    /// </summary>
    public decimal UnitPrice { get; set; }
    
    /// <summary>
    /// Product-specific tax rate (if different from transaction rate).
    /// </summary>
    public decimal TaxRate { get; set; } = 0.14m;
    
    /// <summary>
    /// Line total (Quantity × UnitPrice).
    /// </summary>
    public decimal LineTotal => Quantity * UnitPrice;
    
    /// <summary>
    /// Product barcode/SKU (optional, for detailed invoices).
    /// </summary>
    public string? Barcode { get; set; }
}
