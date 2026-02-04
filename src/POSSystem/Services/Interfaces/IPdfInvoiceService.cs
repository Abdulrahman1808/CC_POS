using System.Threading.Tasks;
using POSSystem.Models;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Interface for generating professional PDF invoices using QuestPDF.
/// </summary>
public interface IPdfInvoiceService
{
    /// <summary>
    /// Generates a PDF invoice and saves it to the specified path.
    /// </summary>
    /// <param name="receipt">The receipt model containing invoice data</param>
    /// <param name="outputPath">Full path where the PDF should be saved</param>
    /// <returns>The full path to the generated PDF file</returns>
    Task<string> GenerateInvoiceAsync(ReceiptModel receipt, string outputPath);
    
    /// <summary>
    /// Generates a PDF invoice and returns it as a byte array.
    /// Useful for in-memory processing, email attachments, or streaming.
    /// </summary>
    /// <param name="receipt">The receipt model containing invoice data</param>
    /// <returns>PDF file as byte array</returns>
    byte[] GenerateInvoiceBytes(ReceiptModel receipt);
    
    /// <summary>
    /// Generates an invoice PDF with auto-generated filename to the default output directory.
    /// </summary>
    /// <param name="receipt">The receipt model containing invoice data</param>
    /// <returns>Full path to the generated PDF</returns>
    Task<string> GenerateInvoiceAsync(ReceiptModel receipt);
}
