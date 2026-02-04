using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models;

/// <summary>
/// Hardware configuration settings for printers, barcode scanners, etc.
/// </summary>
public class HardwareSettings
{
    [Key]
    public int Id { get; set; } = 1; // Singleton row
    
    /// <summary>
    /// Selected Windows printer name for receipts.
    /// </summary>
    [MaxLength(200)]
    public string? ReceiptPrinterName { get; set; }
    
    /// <summary>
    /// Paper width in mm (58 or 80).
    /// </summary>
    public int PaperWidth { get; set; } = 80;
    
    /// <summary>
    /// Auto-print receipt after checkout.
    /// </summary>
    public bool AutoPrintReceipt { get; set; } = false;
    
    /// <summary>
    /// Enable barcode scanner input.
    /// </summary>
    public bool EnableBarcodeScanner { get; set; } = true;
    
    /// <summary>
    /// Open cash drawer on cash payment.
    /// </summary>
    public bool OpenCashDrawerOnPayment { get; set; } = true;
    
    /// <summary>
    /// Number of receipt copies to print.
    /// </summary>
    public int ReceiptCopies { get; set; } = 1;
}
