using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using POSSystem.Helpers;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Facade service for all document generation operations.
/// Coordinates ReceiptModel creation, ETA QR code generation, 
/// thermal printing, and PDF invoice generation.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IThermalReceiptService _thermalService;
    private readonly IPdfInvoiceService _pdfService;
    private readonly ILicenseManager _licenseManager;
    
    public DocumentService(
        IThermalReceiptService thermalService,
        IPdfInvoiceService pdfService,
        ILicenseManager licenseManager)
    {
        _thermalService = thermalService ?? throw new ArgumentNullException(nameof(thermalService));
        _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
        _licenseManager = licenseManager ?? throw new ArgumentNullException(nameof(licenseManager));
    }
    
    /// <summary>
    /// Gets whether the application is running in developer mode.
    /// Developer mode enables debug printing and adds watermarks to documents.
    /// </summary>
    public bool IsDeveloperMode => _licenseManager.IsDeveloperMode;
    
    /// <inheritdoc />
    public ReceiptModel CreateReceiptModel(Transaction transaction, StoreSettings storeSettings)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));
        
        if (storeSettings == null)
            throw new ArgumentNullException(nameof(storeSettings));
        
        // Build receipt model with centralized VAT
        var receipt = new ReceiptModel
        {
            // Store information
            // TODO: Arabic localization - Apply Arabic shaping to StoreName here
            StoreName = storeSettings.StoreName,
            StoreAddress = !string.IsNullOrEmpty(storeSettings.LegalAddress) 
                ? storeSettings.LegalAddress 
                : storeSettings.Address,
            StorePhone = storeSettings.PhoneNumber,
            StoreEmail = storeSettings.Email,
            TaxRegistrationNumber = storeSettings.TaxId,
            LogoPath = storeSettings.LogoPath,
            CurrencySymbol = storeSettings.CurrencySymbol,
            ReceiptFooter = storeSettings.ReceiptFooter,
            
            // Transaction information
            TransactionNumber = transaction.TransactionNumber,
            Timestamp = transaction.CreatedAt,
            PaymentMethod = transaction.PaymentMethod,
            PaymentReference = transaction.PaymentReference,
            CustomerName = transaction.CustomerName,
            CustomerPhone = transaction.CustomerPhone,
            
            // VAT rate from transaction (defaults to 14% Egypt VAT)
            VatRate = transaction.TaxRate,
            
            // Map items
            Items = transaction.Items.Select(i => new ReceiptLineItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TaxRate = transaction.TaxRate,
                Barcode = i.Product?.Barcode
            }).ToList()
        };
        
        // Generate ETA-compliant TLV QR code
        try
        {
            if (!string.IsNullOrEmpty(receipt.TaxRegistrationNumber))
            {
                receipt.EtaTlvBase64 = EtaQrCodeHelper.GenerateTlvBase64(receipt);
                Debug.WriteLine($"[DocumentService] Generated ETA TLV QR: {receipt.EtaTlvBase64.Length} chars");
            }
            else
            {
                Debug.WriteLine("[DocumentService] Tax ID not configured - skipping ETA QR generation");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DocumentService] ETA QR generation failed: {ex.Message}");
        }
        
        return receipt;
    }
    
    /// <inheritdoc />
    public async Task<bool> PrintThermalReceiptAsync(Transaction transaction, StoreSettings storeSettings)
    {
        try
        {
            var receipt = CreateReceiptModel(transaction, storeSettings);
            return await _thermalService.PrintReceiptAsync(receipt);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DocumentService] Thermal print failed: {ex.Message}");
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<string> GeneratePdfInvoiceAsync(Transaction transaction, StoreSettings storeSettings)
    {
        var receipt = CreateReceiptModel(transaction, storeSettings);
        return await _pdfService.GenerateInvoiceAsync(receipt);
    }
    
    /// <inheritdoc />
    public async Task<string> GeneratePdfInvoiceAsync(Transaction transaction, StoreSettings storeSettings, string outputPath)
    {
        var receipt = CreateReceiptModel(transaction, storeSettings);
        return await _pdfService.GenerateInvoiceAsync(receipt, outputPath);
    }
    
    /// <inheritdoc />
    public async Task<(bool Printed, string PdfPath)> GenerateAllDocumentsAsync(Transaction transaction, StoreSettings storeSettings)
    {
        var receipt = CreateReceiptModel(transaction, storeSettings);
        
        // Run both operations in parallel
        var printTask = _thermalService.PrintReceiptAsync(receipt);
        var pdfTask = _pdfService.GenerateInvoiceAsync(receipt);
        
        await Task.WhenAll(printTask, pdfTask);
        
        return (await printTask, await pdfTask);
    }
}
