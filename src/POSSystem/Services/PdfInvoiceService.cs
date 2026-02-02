using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using POSSystem.Models;
using POSSystem.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POSSystem.Services;

/// <summary>
/// Professional PDF invoice generation service using QuestPDF.
/// Generates modern A4 invoices with store branding and ETA QR codes.
/// In Developer Mode, adds a red "TEST INVOICE" watermark.
/// </summary>
public class PdfInvoiceService : IPdfInvoiceService
{
    private readonly ILicenseManager _licenseManager;
    private readonly string _outputDirectory;
    
    /// <summary>
    /// Static constructor to initialize QuestPDF Community License.
    /// </summary>
    static PdfInvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    public PdfInvoiceService(ILicenseManager licenseManager)
    {
        _licenseManager = licenseManager ?? throw new ArgumentNullException(nameof(licenseManager));
        
        // Use Desktop for invoices - more accessible and avoids OneDrive sync issues
        _outputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "POSSystem_Invoices");
        
        try
        {
            Directory.CreateDirectory(_outputDirectory);
        }
        catch (Exception ex)
        {
            // Fallback to temp directory if Desktop fails
            Debug.WriteLine($"[PdfInvoice] Could not create output directory: {ex.Message}");
            _outputDirectory = Path.Combine(Path.GetTempPath(), "POSSystem_Invoices");
            Directory.CreateDirectory(_outputDirectory);
        }
    }
    
    /// <inheritdoc />
    public async Task<string> GenerateInvoiceAsync(ReceiptModel receipt)
    {
        var fileName = $"Invoice_{receipt.TransactionNumber}_{receipt.Timestamp:yyyyMMdd_HHmmss}.pdf";
        var outputPath = Path.Combine(_outputDirectory, fileName);
        return await GenerateInvoiceAsync(receipt, outputPath);
    }
    
    /// <inheritdoc />
    public async Task<string> GenerateInvoiceAsync(ReceiptModel receipt, string outputPath)
    {
        if (receipt == null)
            throw new ArgumentNullException(nameof(receipt));
        
        return await Task.Run(() =>
        {
            try
            {
                var pdfBytes = GenerateInvoiceBytes(receipt);
                
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                File.WriteAllBytes(outputPath, pdfBytes);
                
                Debug.WriteLine($"[PdfInvoiceService] Invoice saved: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PdfInvoiceService] Generation failed: {ex.Message}");
                throw;
            }
        });
    }
    
    /// <inheritdoc />
    public byte[] GenerateInvoiceBytes(ReceiptModel receipt)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));
                
                // Developer Mode Watermark - use simple text background layer
                if (_licenseManager.IsDeveloperMode)
                {
                    page.Background()
                        .AlignCenter()
                        .AlignMiddle()
                        .Text("TEST INVOICE")
                        .FontSize(80)
                        .Bold()
                        .FontColor(Colors.Red.Lighten3);
                }
                
                page.Header().Element(c => ComposeHeader(c, receipt));
                page.Content().Element(c => ComposeContent(c, receipt));
                page.Footer().Element(c => ComposeFooter(c, receipt));
            });
        });
        
        return document.GeneratePdf();
    }
    
    private void ComposeHeader(IContainer container, ReceiptModel receipt)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            
            // Header Row: Logo + Invoice Title
            column.Item().Row(row =>
            {
                // Logo Section
                row.RelativeItem(3).Column(logoCol =>
                {
                    if (!string.IsNullOrEmpty(receipt.LogoPath) && File.Exists(receipt.LogoPath))
                    {
                        logoCol.Item().MaxHeight(60).Image(receipt.LogoPath);
                    }
                    else
                    {
                        logoCol.Item()
                            .Background(Colors.Blue.Darken2)
                            .Padding(10)
                            .Text(receipt.StoreName)
                            .FontSize(16)
                            .Bold()
                            .FontColor(Colors.White);
                    }
                    
                    logoCol.Item().PaddingTop(5).Text(receipt.StoreAddress)
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                    
                    if (!string.IsNullOrEmpty(receipt.StorePhone))
                        logoCol.Item().Text($"Tel: {receipt.StorePhone}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    
                    if (!string.IsNullOrEmpty(receipt.StoreEmail))
                        logoCol.Item().Text($"Email: {receipt.StoreEmail}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                
                row.ConstantItem(20); // Spacer
                
                // Invoice Title Section
                row.RelativeItem(2).Column(titleCol =>
                {
                    titleCol.Item()
                        .AlignRight()
                        .Text("TAX INVOICE")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);
                    
                    titleCol.Item().AlignRight().Text(text =>
                    {
                        text.Span("Invoice #: ").FontSize(10);
                        text.Span(receipt.TransactionNumber).Bold().FontSize(10);
                    });
                    
                    titleCol.Item().AlignRight().Text(text =>
                    {
                        text.Span("Date: ").FontSize(10);
                        text.Span(receipt.Timestamp.ToString("yyyy-MM-dd HH:mm")).FontSize(10);
                    });
                    
                    if (!string.IsNullOrEmpty(receipt.TaxRegistrationNumber))
                    {
                        titleCol.Item().AlignRight()
                            .PaddingTop(5)
                            .Text($"Tax ID: {receipt.TaxRegistrationNumber}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);
                    }
                });
            });
            
            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }
    
    private void ComposeContent(IContainer container, ReceiptModel receipt)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);
            
            // Customer Info (if available)
            if (!string.IsNullOrEmpty(receipt.CustomerName))
            {
                column.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(custCol =>
                {
                    custCol.Item().Text("BILL TO:").Bold().FontSize(9).FontColor(Colors.Grey.Darken1);
                    custCol.Item().Text(receipt.CustomerName).FontSize(11);
                    if (!string.IsNullOrEmpty(receipt.CustomerPhone))
                        custCol.Item().Text($"Phone: {receipt.CustomerPhone}").FontSize(9);
                });
            }
            
            // Items Table
            column.Item().Table(table =>
            {
                // Define columns
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(40);   // #
                    cols.RelativeColumn(4);    // Item
                    cols.RelativeColumn(1);    // Qty
                    cols.RelativeColumn(1.5f); // Unit Price
                    cols.RelativeColumn(1.5f); // Total
                });
                
                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                        .Text("#").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                        .Text("Item Description").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).AlignCenter()
                        .Text("Qty").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).AlignRight()
                        .Text("Unit Price").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).AlignRight()
                        .Text("Total").FontColor(Colors.White).Bold();
                });
                
                // Rows
                var index = 0;
                foreach (var item in receipt.Items)
                {
                    index++;
                    var bgColor = index % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                    
                    table.Cell().Background(bgColor).Padding(8).Text(index.ToString());
                    table.Cell().Background(bgColor).Padding(8).Column(itemCol =>
                    {
                        itemCol.Item().Text(item.ProductName);
                        if (!string.IsNullOrEmpty(item.Barcode))
                            itemCol.Item().Text($"SKU: {item.Barcode}")
                                .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                    table.Cell().Background(bgColor).Padding(8).AlignCenter()
                        .Text(item.Quantity.ToString());
                    table.Cell().Background(bgColor).Padding(8).AlignRight()
                        .Text($"{item.UnitPrice:F2}");
                    table.Cell().Background(bgColor).Padding(8).AlignRight()
                        .Text($"{item.LineTotal:F2}");
                }
            });
            
            // Totals Section
            column.Item().AlignRight().Width(250).Column(totalsCol =>
            {
                totalsCol.Item().Row(row =>
                {
                    row.RelativeItem().Text("Subtotal:").AlignRight();
                    row.ConstantItem(100).Text($"{receipt.SubTotal:F2} {receipt.CurrencySymbol}")
                        .AlignRight();
                });
                
                totalsCol.Item().Row(row =>
                {
                    row.RelativeItem().Text($"VAT ({receipt.VatRateDisplay}):").AlignRight();
                    row.ConstantItem(100).Text($"{receipt.VatAmount:F2} {receipt.CurrencySymbol}")
                        .AlignRight();
                });
                
                totalsCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                
                totalsCol.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("TOTAL:").Bold().FontSize(12).AlignRight();
                    row.ConstantItem(100).Text($"{receipt.Total:F2} {receipt.CurrencySymbol}")
                        .Bold().FontSize(12).AlignRight().FontColor(Colors.Blue.Darken2);
                });
            });
            
            // Payment Information
            column.Item().PaddingTop(20).Background(Colors.Grey.Lighten4).Padding(15).Row(row =>
            {
                row.RelativeItem().Column(payCol =>
                {
                    payCol.Item().Text("PAYMENT DETAILS").Bold().FontSize(10)
                        .FontColor(Colors.Grey.Darken2);
                    payCol.Item().PaddingTop(5).Text($"Method: {receipt.PaymentMethod}");
                    
                    if (!string.IsNullOrEmpty(receipt.PaymentReference))
                        payCol.Item().Text(text =>
                        {
                            text.Span("Reference: ").FontColor(Colors.Grey.Darken1);
                            text.Span(receipt.PaymentReference).Bold();
                        });
                });
                
                // QR Code
                if (!string.IsNullOrEmpty(receipt.EtaTlvBase64))
                {
                    row.ConstantItem(120).Column(qrCol =>
                    {
                        qrCol.Item().AlignCenter().Text("ETA Verification").FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                        qrCol.Item().PaddingTop(5).AlignCenter().Element(qrContainer =>
                        {
                            var qrBytes = GenerateQrCodeImage(receipt.EtaTlvBase64);
                            qrContainer.Width(80).Height(80).Image(qrBytes);
                        });
                    });
                }
            });
        });
    }
    
    private void ComposeFooter(IContainer container, ReceiptModel receipt)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span(receipt.ReceiptFooter).FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(9);
                    text.CurrentPageNumber().FontSize(9);
                    text.Span(" of ").FontSize(9);
                    text.TotalPages().FontSize(9);
                });
            });
            
            // Developer Mode Indicator
            if (_licenseManager.IsDeveloperMode)
            {
                column.Item().PaddingTop(10).AlignCenter()
                    .Text("⚠️ DEVELOPER MODE - NOT FOR PRODUCTION USE")
                    .FontSize(8)
                    .FontColor(Colors.Red.Darken1);
            }
        });
    }
    
    /// <summary>
    /// Generates a QR code image as PNG bytes for embedding in PDF.
    /// </summary>
    private static byte[] GenerateQrCodeImage(string data)
    {
        using var qrGenerator = new QRCoder.QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCoder.QRCodeGenerator.ECCLevel.M);
        using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(4);
    }
}
