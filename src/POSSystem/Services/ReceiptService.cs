using System;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using POSSystem.Models;
using QRCoder;

namespace POSSystem.Services;

/// <summary>
/// Service for thermal receipt printing using ESC/POS commands.
/// Supports 58mm and 80mm thermal printers.
/// </summary>
public class ReceiptService
{
    private const int CHARS_PER_LINE_58MM = 32;
    private const int CHARS_PER_LINE_80MM = 48;
    
    private string _printerName = "";
    private int _paperWidth = 80; // mm
    
    /// <summary>
    /// Gets or sets the selected printer name.
    /// </summary>
    public string PrinterName
    {
        get => _printerName;
        set => _printerName = value;
    }
    
    /// <summary>
    /// Gets or sets the paper width (58 or 80mm).
    /// </summary>
    public int PaperWidth
    {
        get => _paperWidth;
        set => _paperWidth = value == 58 ? 58 : 80;
    }
    
    private int CharsPerLine => PaperWidth == 58 ? CHARS_PER_LINE_58MM : CHARS_PER_LINE_80MM;
    
    /// <summary>
    /// Gets available Windows printers.
    /// </summary>
    public static string[] GetAvailablePrinters()
    {
        var printers = new string[PrinterSettings.InstalledPrinters.Count];
        PrinterSettings.InstalledPrinters.CopyTo(printers, 0);
        return printers;
    }
    
    /// <summary>
    /// Prints a receipt for the given transaction asynchronously.
    /// </summary>
    public async Task PrintReceiptAsync(Transaction transaction, StoreSettings? storeSettings)
    {
        await Task.Run(() => PrintReceipt(transaction, storeSettings));
    }
    
    private void PrintReceipt(Transaction transaction, StoreSettings? storeSettings)
    {
        if (string.IsNullOrEmpty(_printerName))
        {
            Debug.WriteLine("[ReceiptService] No printer configured");
            return;
        }
        
        try
        {
            var receipt = FormatReceipt(transaction, storeSettings);
            SendToPrinter(receipt);
            Debug.WriteLine($"[ReceiptService] Receipt printed successfully on {_printerName}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReceiptService] Print error: {ex.Message}");
        }
    }
    
    private byte[] FormatReceipt(Transaction transaction, StoreSettings? storeSettings)
    {
        using var ms = new MemoryStream();
        
        // ESC/POS Commands
        Write(ms, ESC.Initialize);
        
        // Store Header
        Write(ms, ESC.CenterAlign);
        Write(ms, ESC.DoubleHeight);
        WriteLine(ms, storeSettings?.StoreName ?? "POS System");
        Write(ms, ESC.NormalText);
        
        if (!string.IsNullOrEmpty(storeSettings?.Address))
        {
            WriteLine(ms, storeSettings.Address);
        }
        
        if (!string.IsNullOrEmpty(storeSettings?.PhoneNumber))
        {
            WriteLine(ms, $"Tel: {storeSettings.PhoneNumber}");
        }
        
        // Tax ID (ETA Compliance)
        if (!string.IsNullOrEmpty(storeSettings?.TaxId))
        {
            WriteLine(ms, $"Tax ID: {storeSettings.TaxId}");
        }
        
        Write(ms, ESC.LeftAlign);
        WriteLine(ms, new string('-', CharsPerLine));
        
        // Transaction Info
        WriteLine(ms, $"Receipt: {transaction.TransactionNumber}");
        WriteLine(ms, $"Date: {transaction.CreatedAt:yyyy-MM-dd HH:mm}");
        WriteLine(ms, new string('-', CharsPerLine));
        
        // Items
        Write(ms, ESC.BoldOn);
        WriteLine(ms, FormatLine("Item", "Qty", "Price"));
        Write(ms, ESC.BoldOff);
        
        foreach (var item in transaction.Items)
        {
            var name = item.ProductName.Length > 16 
                ? item.ProductName[..16] 
                : item.ProductName;
            WriteLine(ms, FormatLine(name, item.Quantity.ToString(), $"{item.UnitPrice:F2}"));
            
            if (item.Quantity > 1)
            {
                WriteLine(ms, FormatLine("", "", $"= {item.LineTotal:F2}"));
            }
        }
        
        WriteLine(ms, new string('-', CharsPerLine));
        
        // Totals
        WriteLine(ms, FormatLine("Subtotal:", "", $"{transaction.SubTotal:F2}"));
        WriteLine(ms, FormatLine($"Tax ({transaction.TaxRate:P0}):", "", $"{transaction.TaxAmount:F2}"));
        
        Write(ms, ESC.DoubleHeight);
        WriteLine(ms, FormatLine("TOTAL:", "", $"{transaction.Total:F2}"));
        Write(ms, ESC.NormalText);
        
        // Payment Info
        WriteLine(ms, new string('-', CharsPerLine));
        WriteLine(ms, $"Payment: {transaction.PaymentMethod}");
        
        // Fawry Reference (if applicable)
        if (!string.IsNullOrEmpty(transaction.PaymentReference))
        {
            Write(ms, ESC.BoldOn);
            WriteLine(ms, $"Ref: {transaction.PaymentReference}");
            Write(ms, ESC.BoldOff);
        }
        
        WriteLine(ms, new string('-', CharsPerLine));
        
        // QR Code (ETA Compliance)
        Write(ms, ESC.CenterAlign);
        var qrData = GenerateEtaQrData(transaction, storeSettings);
        WriteQrCode(ms, qrData);
        
        // Footer
        WriteLine(ms, "");
        WriteLine(ms, "Thank you for your purchase!");
        WriteLine(ms, storeSettings?.ReceiptFooter ?? "");
        
        // Cut Paper
        Write(ms, ESC.FeedAndCut);
        
        return ms.ToArray();
    }
    
    private string GenerateEtaQrData(Transaction transaction, StoreSettings? storeSettings)
    {
        // ETA E-Receipt QR Format
        var data = new StringBuilder();
        data.AppendLine($"Store: {storeSettings?.StoreName ?? "POS"}");
        data.AppendLine($"Tax ID: {storeSettings?.TaxId ?? "N/A"}");
        data.AppendLine($"Date: {transaction.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        data.AppendLine($"Total: {transaction.Total:F2} EGP");
        data.AppendLine($"Ref: {transaction.TransactionNumber}");
        return data.ToString();
    }
    
    private void WriteQrCode(MemoryStream ms, string data)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
            
            // ESC/POS native QR code commands (supported by most thermal printers)
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var len = dataBytes.Length + 3;
            
            // GS ( k - QR Code commands
            ms.WriteByte(0x1D); // GS
            ms.WriteByte(0x28); // (
            ms.WriteByte(0x6B); // k
            ms.WriteByte((byte)(len % 256));
            ms.WriteByte((byte)(len / 256));
            ms.WriteByte(0x31); // cn
            ms.WriteByte(0x50); // fn (store data)
            ms.WriteByte(0x30); // m
            ms.Write(dataBytes, 0, dataBytes.Length);
            
            // Print QR
            ms.WriteByte(0x1D);
            ms.WriteByte(0x28);
            ms.WriteByte(0x6B);
            ms.WriteByte(0x03);
            ms.WriteByte(0x00);
            ms.WriteByte(0x31);
            ms.WriteByte(0x51);
            ms.WriteByte(0x30);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReceiptService] QR generation failed: {ex.Message}");
        }
    }
    
    private string FormatLine(string left, string mid, string right)
    {
        var totalChars = CharsPerLine;
        var leftLen = Math.Min(left.Length, totalChars / 2);
        var rightLen = Math.Min(right.Length, totalChars / 4);
        var midLen = Math.Min(mid.Length, 4);
        
        var spaces = totalChars - leftLen - midLen - rightLen;
        return $"{left.PadRight(leftLen)}{new string(' ', Math.Max(1, spaces / 2))}{mid}{new string(' ', Math.Max(1, spaces - spaces / 2))}{right}";
    }
    
    private void Write(MemoryStream ms, byte[] data) => ms.Write(data, 0, data.Length);
    private void WriteLine(MemoryStream ms, string text)
    {
        var bytes = Encoding.GetEncoding(437).GetBytes(text + "\n");
        ms.Write(bytes, 0, bytes.Length);
    }
    
    private void SendToPrinter(byte[] data)
    {
        // Windows RAW printing
        if (!RawPrinterHelper.SendBytesToPrinter(_printerName, data))
        {
            throw new Exception("Failed to send data to printer");
        }
    }
}

/// <summary>
/// ESC/POS command constants.
/// </summary>
internal static class ESC
{
    public static readonly byte[] Initialize = { 0x1B, 0x40 }; // ESC @
    public static readonly byte[] CenterAlign = { 0x1B, 0x61, 0x01 }; // ESC a 1
    public static readonly byte[] LeftAlign = { 0x1B, 0x61, 0x00 }; // ESC a 0
    public static readonly byte[] RightAlign = { 0x1B, 0x61, 0x02 }; // ESC a 2
    public static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 }; // ESC E 1
    public static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 }; // ESC E 0
    public static readonly byte[] DoubleHeight = { 0x1B, 0x21, 0x10 }; // ESC ! 16
    public static readonly byte[] NormalText = { 0x1B, 0x21, 0x00 }; // ESC ! 0
    public static readonly byte[] FeedAndCut = { 0x1D, 0x56, 0x41, 0x03 }; // GS V A 3
}

/// <summary>
/// Helper for raw printer access on Windows.
/// </summary>
internal static class RawPrinterHelper
{
    [System.Runtime.InteropServices.DllImport("winspool.drv", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int Level, ref DOCINFOA pDocInfo);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);
    
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten);
    
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct DOCINFOA
    {
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
        public string pDocName;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
        public string? pOutputFile;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
        public string? pDataType;
    }
    
    public static bool SendBytesToPrinter(string printerName, byte[] bytes)
    {
        IntPtr hPrinter = IntPtr.Zero;
        DOCINFOA di = new()
        {
            pDocName = "POS Receipt",
            pDataType = "RAW"
        };
        
        bool success = false;
        
        if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
        {
            if (StartDocPrinter(hPrinter, 1, ref di))
            {
                if (StartPagePrinter(hPrinter))
                {
                    success = WritePrinter(hPrinter, bytes, bytes.Length, out _);
                    EndPagePrinter(hPrinter);
                }
                EndDocPrinter(hPrinter);
            }
            ClosePrinter(hPrinter);
        }
        
        return success;
    }
}
