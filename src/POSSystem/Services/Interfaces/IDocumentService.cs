using System.Threading.Tasks;
using POSSystem.Models;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Facade service for all document generation operations.
/// Coordinates ReceiptModel creation, ETA QR code generation, 
/// thermal printing, and PDF invoice generation.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Creates a ReceiptModel from a Transaction and StoreSettings.
    /// Centralizes VAT calculations and generates ETA-compliant QR code.
    /// </summary>
    /// <param name="transaction">The transaction to convert</param>
    /// <param name="storeSettings">Store configuration</param>
    /// <returns>Fully populated ReceiptModel with ETA TLV QR code</returns>
    ReceiptModel CreateReceiptModel(Transaction transaction, StoreSettings storeSettings);
    
    /// <summary>
    /// Prints a thermal receipt for the given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to print</param>
    /// <param name="storeSettings">Store configuration</param>
    /// <returns>True if printing succeeded</returns>
    Task<bool> PrintThermalReceiptAsync(Transaction transaction, StoreSettings storeSettings);
    
    /// <summary>
    /// Generates a PDF invoice for the given transaction.
    /// </summary>
    /// <param name="transaction">The transaction</param>
    /// <param name="storeSettings">Store configuration</param>
    /// <returns>Full path to the generated PDF file</returns>
    Task<string> GeneratePdfInvoiceAsync(Transaction transaction, StoreSettings storeSettings);
    
    /// <summary>
    /// Generates a PDF invoice to a specific location.
    /// </summary>
    /// <param name="transaction">The transaction</param>
    /// <param name="storeSettings">Store configuration</param>
    /// <param name="outputPath">Target file path for the PDF</param>
    /// <returns>Full path to the generated PDF file</returns>
    Task<string> GeneratePdfInvoiceAsync(Transaction transaction, StoreSettings storeSettings, string outputPath);
    
    /// <summary>
    /// Prints receipt AND generates PDF invoice in one operation.
    /// </summary>
    /// <param name="transaction">The transaction</param>
    /// <param name="storeSettings">Store configuration</param>
    /// <returns>Tuple of (print success, PDF path)</returns>
    Task<(bool Printed, string PdfPath)> GenerateAllDocumentsAsync(Transaction transaction, StoreSettings storeSettings);
}
