using System.Threading.Tasks;
using POSSystem.Models;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Interface for thermal receipt printing using ESC/POS commands.
/// Supports 58mm and 80mm thermal printers.
/// </summary>
public interface IThermalReceiptService
{
    /// <summary>
    /// Prints a receipt asynchronously to the configured printer.
    /// </summary>
    /// <param name="receipt">The receipt model to print</param>
    /// <returns>True if printing succeeded, false otherwise</returns>
    Task<bool> PrintReceiptAsync(ReceiptModel receipt);
    
    /// <summary>
    /// Generates the raw ESC/POS byte array for a receipt without printing.
    /// Useful for testing or saving to file.
    /// </summary>
    /// <param name="receipt">The receipt model</param>
    /// <returns>Raw ESC/POS command bytes</returns>
    byte[] GenerateReceiptBytes(ReceiptModel receipt);
    
    /// <summary>
    /// Gets a list of available Windows printers.
    /// </summary>
    /// <returns>Array of printer names</returns>
    string[] GetAvailablePrinters();
    
    /// <summary>
    /// Sets the target printer and paper width.
    /// </summary>
    /// <param name="printerName">Windows printer name</param>
    /// <param name="paperWidth">Paper width in mm (58 or 80)</param>
    void SetPrinter(string printerName, int paperWidth = 80);
    
    /// <summary>
    /// Gets or sets the configured printer name.
    /// </summary>
    string PrinterName { get; }
    
    /// <summary>
    /// Gets or sets the paper width (58 or 80mm).
    /// </summary>
    int PaperWidth { get; }
}
