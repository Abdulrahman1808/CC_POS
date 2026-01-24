using System;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using POSSystem.Models;
using POSSystem.Services.Interfaces;
using QRCoder;

namespace POSSystem.Services;

/// <summary>
/// Thermal receipt printer service using ESCPOS_NET library.
/// Supports 58mm and 80mm thermal printers with optimized QR code density.
/// In Developer Mode, prints to Debug console instead of physical printer.
/// </summary>
public class ThermalReceiptService : IThermalReceiptService, IDisposable
{
    private const int CHARS_PER_LINE_58MM = 32;
    private const int CHARS_PER_LINE_80MM = 48;
    
    private readonly ILicenseManager _licenseManager;
    private BasePrinter? _printer;
    private string _printerName = string.Empty;
    private int _paperWidth = 80;
    
    public ThermalReceiptService(ILicenseManager licenseManager)
    {
        _licenseManager = licenseManager ?? throw new ArgumentNullException(nameof(licenseManager));
    }
    
    /// <inheritdoc />
    public string PrinterName => _printerName;
    
    /// <inheritdoc />
    public int PaperWidth => _paperWidth;
    
    private int CharsPerLine => _paperWidth == 58 ? CHARS_PER_LINE_58MM : CHARS_PER_LINE_80MM;
    
    /// <inheritdoc />
    public void SetPrinter(string printerName, int paperWidth = 80)
    {
        _printerName = printerName;
        _paperWidth = paperWidth == 58 ? 58 : 80;
        
        // Close existing printer connection
        _printer?.Dispose();
        _printer = null;
        
        Debug.WriteLine($"[ThermalReceiptService] Printer set: {printerName} ({_paperWidth}mm)");
    }
    
    /// <inheritdoc />
    public string[] GetAvailablePrinters()
    {
        var printers = new string[PrinterSettings.InstalledPrinters.Count];
        PrinterSettings.InstalledPrinters.CopyTo(printers, 0);
        return printers;
    }
    
    /// <inheritdoc />
    public async Task<bool> PrintReceiptAsync(ReceiptModel receipt)
    {
        if (receipt == null)
            throw new ArgumentNullException(nameof(receipt));
        
        try
        {
            var receiptBytes = GenerateReceiptBytes(receipt);
            
            // Developer Mode: Print to console/debug instead of physical printer
            if (_licenseManager.IsDeveloperMode)
            {
                PrintToDebugConsole(receipt, receiptBytes);
                return true;
            }
            
            // Production Mode: Send to physical printer
            if (string.IsNullOrEmpty(_printerName))
            {
                Debug.WriteLine("[ThermalReceiptService] No printer configured");
                return false;
            }
            
            return await Task.Run(() => SendToPrinter(receiptBytes));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ThermalReceiptService] Print error: {ex.Message}");
            return false;
        }
    }
    
    /// <inheritdoc />
    public byte[] GenerateReceiptBytes(ReceiptModel receipt)
    {
        var e = new EPSON();
        using var ms = new MemoryStream();
        
        void Write(byte[] data) => ms.Write(data, 0, data.Length);
        
        // Initialize printer
        Write(e.Initialize());
        Write(e.Enable());
        
        // ========== HEADER ==========
        Write(e.CenterAlign());
        Write(e.SetStyles(PrintStyle.DoubleHeight | PrintStyle.Bold));
        
        // Store name (TODO: Arabic localization - apply shaping here)
        Write(e.PrintLine(receipt.StoreName));
        
        Write(e.SetStyles(PrintStyle.None));
        
        if (!string.IsNullOrEmpty(receipt.StoreAddress))
            Write(e.PrintLine(receipt.StoreAddress));
        
        if (!string.IsNullOrEmpty(receipt.StorePhone))
            Write(e.PrintLine($"Tel: {receipt.StorePhone}"));
        
        // Tax ID (ETA Compliance)
        if (!string.IsNullOrEmpty(receipt.TaxRegistrationNumber))
            Write(e.PrintLine($"Tax ID: {receipt.TaxRegistrationNumber}"));
        
        Write(e.LeftAlign());
        Write(e.PrintLine(new string('-', CharsPerLine)));
        
        // ========== TRANSACTION INFO ==========
        Write(e.PrintLine($"Receipt: {receipt.TransactionNumber}"));
        Write(e.PrintLine($"Date: {receipt.Timestamp:yyyy-MM-dd HH:mm}"));
        
        if (!string.IsNullOrEmpty(receipt.CashierName))
            Write(e.PrintLine($"Cashier: {receipt.CashierName}"));
        
        Write(e.PrintLine(new string('-', CharsPerLine)));
        
        // ========== ITEMS TABLE ==========
        Write(e.SetStyles(PrintStyle.Bold));
        Write(e.PrintLine(FormatTableRow("Item", "Qty", "Price", CharsPerLine)));
        Write(e.SetStyles(PrintStyle.None));
        
        foreach (var item in receipt.Items)
        {
            var name = TruncateString(item.ProductName, CharsPerLine / 2);
            Write(e.PrintLine(FormatTableRow(
                name, 
                item.Quantity.ToString(), 
                $"{item.UnitPrice:F2}",
                CharsPerLine)));
            
            // Show line total for multi-quantity items
            if (item.Quantity > 1)
            {
                Write(e.PrintLine(FormatTableRow("", "", $"= {item.LineTotal:F2}", CharsPerLine)));
            }
        }
        
        Write(e.PrintLine(new string('-', CharsPerLine)));
        
        // ========== TOTALS ==========
        Write(e.PrintLine(FormatTotalRow("Subtotal:", $"{receipt.SubTotal:F2} {receipt.CurrencySymbol}", CharsPerLine)));
        Write(e.PrintLine(FormatTotalRow($"VAT ({receipt.VatRateDisplay}):", $"{receipt.VatAmount:F2} {receipt.CurrencySymbol}", CharsPerLine)));
        
        Write(e.SetStyles(PrintStyle.DoubleHeight | PrintStyle.Bold));
        Write(e.PrintLine(FormatTotalRow("TOTAL:", $"{receipt.Total:F2} {receipt.CurrencySymbol}", CharsPerLine)));
        Write(e.SetStyles(PrintStyle.None));
        
        // ========== PAYMENT INFO ==========
        Write(e.PrintLine(new string('-', CharsPerLine)));
        Write(e.PrintLine($"Payment: {receipt.PaymentMethod}"));
        
        // Fawry Reference (Mobile payments)
        if (!string.IsNullOrEmpty(receipt.PaymentReference))
        {
            Write(e.SetStyles(PrintStyle.Bold));
            Write(e.PrintLine($"Ref: {receipt.PaymentReference}"));
            Write(e.SetStyles(PrintStyle.None));
        }
        
        Write(e.PrintLine(new string('-', CharsPerLine)));
        
        // ========== ETA QR CODE ==========
        Write(e.CenterAlign());
        
        if (!string.IsNullOrEmpty(receipt.EtaTlvBase64))
        {
            // QR_Model2 with optimized size for 80mm/58mm scannability
            var qrSize = _paperWidth == 58 ? 4 : 6; // Module size
            Write(GenerateQrCodeBytes(receipt.EtaTlvBase64, qrSize));
        }
        
        // ========== FOOTER ==========
        Write(e.PrintLine(""));
        Write(e.PrintLine("Thank you for your purchase!"));
        
        if (!string.IsNullOrEmpty(receipt.ReceiptFooter) && 
            receipt.ReceiptFooter != "Thank you for your purchase!")
        {
            Write(e.PrintLine(receipt.ReceiptFooter));
        }
        
        // Feed and cut
        Write(e.FeedLines(3));
        Write(e.PartialCutAfterFeed(2));
        
        return ms.ToArray();
    }
    
    /// <summary>
    /// Generates ESC/POS QR code commands using QR_Model2 for optimal density.
    /// </summary>
    private byte[] GenerateQrCodeBytes(string data, int moduleSize = 6)
    {
        using var ms = new MemoryStream();
        var dataBytes = Encoding.UTF8.GetBytes(data);
        
        // Function 165: QR Code Model Selection (Model 2)
        ms.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 }, 0, 9);
        
        // Function 167: Set QR Code Size (module size)
        ms.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, (byte)moduleSize }, 0, 8);
        
        // Function 169: Set Error Correction Level (M = 48, L = 49, Q = 50, H = 51)
        ms.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31 }, 0, 8); // Level M
        
        // Function 180: Store QR Code Data
        var storeLen = dataBytes.Length + 3;
        ms.Write(new byte[] { 
            0x1D, 0x28, 0x6B, 
            (byte)(storeLen % 256), (byte)(storeLen / 256),
            0x31, 0x50, 0x30 
        }, 0, 8);
        ms.Write(dataBytes, 0, dataBytes.Length);
        
        // Function 181: Print QR Code
        ms.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 }, 0, 8);
        
        return ms.ToArray();
    }
    
    /// <summary>
    /// Prints receipt to Debug console for development testing.
    /// </summary>
    private void PrintToDebugConsole(ReceiptModel receipt, byte[] rawBytes)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("╔════════════════════════════════════════════════╗");
        sb.AppendLine("║     🖨️ DEVELOPER MODE - RECEIPT PREVIEW        ║");
        sb.AppendLine("╠════════════════════════════════════════════════╣");
        sb.AppendLine($"║  {CenterString(receipt.StoreName, 44)}  ║");
        sb.AppendLine($"║  {CenterString(receipt.StoreAddress, 44)}  ║");
        sb.AppendLine($"║  {CenterString($"Tax ID: {receipt.TaxRegistrationNumber}", 44)}  ║");
        sb.AppendLine("╠════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Receipt: {receipt.TransactionNumber,-35}  ║");
        sb.AppendLine($"║  Date: {receipt.Timestamp:yyyy-MM-dd HH:mm,-38}  ║");
        sb.AppendLine("╠════════════════════════════════════════════════╣");
        
        foreach (var item in receipt.Items)
        {
            sb.AppendLine($"║  {item.ProductName,-25} x{item.Quantity,-3} {item.LineTotal,10:F2}  ║");
        }
        
        sb.AppendLine("╠════════════════════════════════════════════════╣");
        sb.AppendLine($"║  {"Subtotal:",-36} {receipt.SubTotal,7:F2}  ║");
        sb.AppendLine($"║  {$"VAT ({receipt.VatRateDisplay}):",-36} {receipt.VatAmount,7:F2}  ║");
        sb.AppendLine($"║  {"TOTAL:",-36} {receipt.Total,7:F2}  ║");
        sb.AppendLine("╠════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Payment: {receipt.PaymentMethod,-35}  ║");
        
        if (!string.IsNullOrEmpty(receipt.PaymentReference))
            sb.AppendLine($"║  Ref: {receipt.PaymentReference,-39}  ║");
        
        sb.AppendLine("╠════════════════════════════════════════════════╣");
        sb.AppendLine($"║  {CenterString("[QR CODE]", 44)}  ║");
        sb.AppendLine($"║  {CenterString($"TLV: {receipt.EtaTlvBase64.Substring(0, Math.Min(30, receipt.EtaTlvBase64.Length))}...", 44)}  ║");
        sb.AppendLine("╠════════════════════════════════════════════════╣");
        sb.AppendLine($"║  {CenterString("Thank you for your purchase!", 44)}  ║");
        sb.AppendLine("╚════════════════════════════════════════════════╝");
        sb.AppendLine($"[RAW BYTES: {rawBytes.Length} bytes]");
        
        Debug.WriteLine(sb.ToString());
    }
    
    /// <summary>
    /// Sends raw bytes to the physical printer.
    /// </summary>
    private bool SendToPrinter(byte[] data)
    {
        try
        {
            // Use Windows RAW printing
            return RawPrinterHelper.SendBytesToPrinter(_printerName, data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ThermalReceiptService] Printer error: {ex.Message}");
            return false;
        }
    }
    
    #region Formatting Helpers
    
    private static string FormatTableRow(string col1, string col2, string col3, int totalWidth)
    {
        var col1Width = totalWidth / 2;
        var col2Width = 6;
        var col3Width = totalWidth - col1Width - col2Width;
        
        return $"{col1.PadRight(col1Width)}{col2.PadLeft(col2Width)}{col3.PadLeft(col3Width)}";
    }
    
    private static string FormatTotalRow(string label, string value, int totalWidth)
    {
        var labelWidth = totalWidth - value.Length - 2;
        return $"{label.PadRight(labelWidth)}  {value}";
    }
    
    private static string TruncateString(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength - 2) + "..";
    }
    
    private static string CenterString(string text, int width)
    {
        if (string.IsNullOrEmpty(text))
            return new string(' ', width);
        
        if (text.Length >= width)
            return text.Substring(0, width);
        
        var padding = (width - text.Length) / 2;
        return text.PadLeft(padding + text.Length).PadRight(width);
    }
    
    #endregion
    
    public void Dispose()
    {
        _printer?.Dispose();
    }
}

