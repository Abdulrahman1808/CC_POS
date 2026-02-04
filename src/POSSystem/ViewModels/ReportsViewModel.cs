using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSSystem.Data.Interfaces;
using POSSystem.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POSSystem.ViewModels;

/// <summary>
/// Report data for display.
/// </summary>
public class SalesReportItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Count { get; set; }
}

public class TopProductItem
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class EmployeeSalesItem
{
    public string EmployeeName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalSales { get; set; }
}

/// <summary>
/// ViewModel for Reports with daily/weekly analytics.
/// </summary>
public partial class ReportsViewModel : ObservableObject
{
    private readonly IDataService _dataService;

    /// <summary>
    /// Static constructor to initialize QuestPDF Community License.
    /// </summary>
    static ReportsViewModel()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [ObservableProperty]
    private string _selectedPeriod = "Today";

    [ObservableProperty]
    private decimal _totalRevenue;

    [ObservableProperty]
    private decimal _totalCost;

    [ObservableProperty]
    private decimal _grossProfit;

    [ObservableProperty]
    private int _totalTransactions;

    [ObservableProperty]
    private decimal _averageOrderValue;

    [ObservableProperty]
    private ObservableCollection<TopProductItem> _topProducts = new();

    [ObservableProperty]
    private ObservableCollection<EmployeeSalesItem> _employeeSales = new();

    [ObservableProperty]
    private bool _isLoading;

    public string[] Periods => new[] { "Today", "This Week", "This Month" };

    public ReportsViewModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    #region Navigation
    public event EventHandler<string>? NavigationRequested;

    [RelayCommand]
    private void BackToDashboard() => NavigationRequested?.Invoke(this, "Dashboard");
    #endregion

    public async Task InitializeAsync()
    {
        await LoadReportsAsync();
    }

    partial void OnSelectedPeriodChanged(string value) => _ = LoadReportsAsync();

    [RelayCommand]
    private async Task LoadReportsAsync()
    {
        IsLoading = true;
        try
        {
            var (startDate, endDate) = GetDateRange();
            Debug.WriteLine($"[Reports] Loading {SelectedPeriod}: {startDate:d} to {endDate:d}");

            var transactions = await _dataService.GetTransactionsAsync(startDate, endDate);
            var transactionList = transactions.ToList();

            // Calculate totals
            TotalRevenue = transactionList.Sum(t => t.Total);
            TotalTransactions = transactionList.Count;
            AverageOrderValue = TotalTransactions > 0 ? TotalRevenue / TotalTransactions : 0;

            // Calculate cost and profit (from items)
            TotalCost = 0;
            foreach (var trans in transactionList)
            {
                foreach (var item in trans.Items)
                {
                    var product = await _dataService.GetProductByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        TotalCost += product.Cost * item.Quantity;
                    }
                }
            }
            GrossProfit = TotalRevenue - TotalCost;

            // Top products
            var productSales = transactionList
                .SelectMany(t => t.Items)
                .GroupBy(i => i.ProductName)
                .Select(g => new TopProductItem
                {
                    ProductName = g.Key,
                    QuantitySold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.UnitPrice * i.Quantity)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToList();

            TopProducts = new ObservableCollection<TopProductItem>(productSales);

            // Employee sales (placeholder - needs staff tracking per transaction)
            var employeeSales = new ObservableCollection<EmployeeSalesItem>
            {
                new() { EmployeeName = "All Staff", TransactionCount = TotalTransactions, TotalSales = TotalRevenue }
            };
            EmployeeSales = employeeSales;

            Debug.WriteLine($"[Reports] Revenue: {TotalRevenue:C}, Profit: {GrossProfit:C}, Top Products: {TopProducts.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Reports] Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private (DateTime start, DateTime end) GetDateRange()
    {
        var now = DateTime.UtcNow;
        return SelectedPeriod switch
        {
            "Today" => (now.Date, now.Date.AddDays(1)),
            "This Week" => (now.Date.AddDays(-(int)now.DayOfWeek), now.Date.AddDays(1)),
            "This Month" => (new DateTime(now.Year, now.Month, 1), now.Date.AddDays(1)),
            _ => (now.Date, now.Date.AddDays(1))
        };
    }

    [RelayCommand]
    private async Task ExportToPdfAsync()
    {
        try
        {
            IsLoading = true;
            var (startDate, endDate) = GetDateRange();
            var fileName = $"SalesReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            // Generate PDF using QuestPDF
            await Task.Run(() => GeneratePdfReport(filePath, startDate, endDate));

            MessageBox.Show($"Report exported to:\n{filePath}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Open file
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Reports] PDF export error: {ex.Message}");
            MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        try
        {
            IsLoading = true;
            var (startDate, endDate) = GetDateRange();
            var fileName = $"SalesReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            await Task.Run(() => GenerateExcelReport(filePath, startDate, endDate));

            MessageBox.Show($"Report exported to:\n{filePath}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Open file
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Reports] Excel export error: {ex.Message}");
            MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void GeneratePdfReport(string filePath, DateTime startDate, DateTime endDate)
    {
        Debug.WriteLine($"[Reports] Generating PDF: {filePath}");
        
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(c => ComposeHeader(c, startDate, endDate));
                page.Content().Element(c => ComposeContent(c));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(filePath);
        
        Debug.WriteLine($"[Reports] PDF generated successfully");
    }

    private void ComposeHeader(IContainer container, DateTime startDate, DateTime endDate)
    {
        container.Column(column =>
        {
            column.Item().Text("Sales Report")
                .FontSize(24)
                .Bold()
                .FontColor(Colors.Blue.Darken2);
            
            column.Item().Text($"Period: {startDate:MMMM d, yyyy} to {endDate:MMMM d, yyyy}")
                .FontSize(12)
                .FontColor(Colors.Grey.Darken1);
            
            column.Item().PaddingBottom(10);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            // Summary section
            column.Item().PaddingBottom(15).Element(ComposeSummaryTable);
            
            // Top Products section
            column.Item().Text("Top Products").FontSize(16).Bold().FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5).Element(ComposeProductsTable);
        });
    }

    private void ComposeSummaryTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Metric").Bold();
            table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Value").Bold();

            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Total Revenue");
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{TotalRevenue:C}");

            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Total Cost");
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{TotalCost:C}");

            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Gross Profit");
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                .Text($"{GrossProfit:C}").FontColor(GrossProfit >= 0 ? Colors.Green.Darken1 : Colors.Red.Darken1);

            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Total Transactions");
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{TotalTransactions}");

            table.Cell().Padding(5).Text("Avg Order Value");
            table.Cell().Padding(5).Text($"{AverageOrderValue:C}");
        });
    }

    private void ComposeProductsTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Product").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Qty Sold").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Revenue").Bold();
            });

            foreach (var product in TopProducts)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(product.ProductName);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{product.QuantitySold}");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{product.Revenue:C}");
            }
        });
    }

    private void GenerateExcelReport(string filePath, DateTime startDate, DateTime endDate)
    {
        Debug.WriteLine($"[Reports] Generating Excel: {filePath}");
        
        using var workbook = new XLWorkbook();
        
        // Summary sheet
        var summarySheet = workbook.Worksheets.Add("Summary");
        summarySheet.Cell("A1").Value = "Sales Report";
        summarySheet.Cell("A1").Style.Font.Bold = true;
        summarySheet.Cell("A1").Style.Font.FontSize = 16;
        
        summarySheet.Cell("A2").Value = $"Period: {startDate:d} to {endDate:d}";
        
        summarySheet.Cell("A4").Value = "Metric";
        summarySheet.Cell("B4").Value = "Value";
        summarySheet.Range("A4:B4").Style.Fill.BackgroundColor = XLColor.LightBlue;
        summarySheet.Range("A4:B4").Style.Font.Bold = true;
        
        summarySheet.Cell("A5").Value = "Total Revenue";
        summarySheet.Cell("B5").Value = TotalRevenue;
        summarySheet.Cell("B5").Style.NumberFormat.Format = "£#,##0.00";
        
        summarySheet.Cell("A6").Value = "Total Cost";
        summarySheet.Cell("B6").Value = TotalCost;
        summarySheet.Cell("B6").Style.NumberFormat.Format = "£#,##0.00";
        
        summarySheet.Cell("A7").Value = "Gross Profit";
        summarySheet.Cell("B7").Value = GrossProfit;
        summarySheet.Cell("B7").Style.NumberFormat.Format = "£#,##0.00";
        summarySheet.Cell("B7").Style.Font.FontColor = GrossProfit >= 0 ? XLColor.Green : XLColor.Red;
        
        summarySheet.Cell("A8").Value = "Total Transactions";
        summarySheet.Cell("B8").Value = TotalTransactions;
        
        summarySheet.Cell("A9").Value = "Average Order Value";
        summarySheet.Cell("B9").Value = AverageOrderValue;
        summarySheet.Cell("B9").Style.NumberFormat.Format = "£#,##0.00";
        
        summarySheet.Columns().AdjustToContents();
        
        // Top Products sheet
        var productsSheet = workbook.Worksheets.Add("Top Products");
        productsSheet.Cell("A1").Value = "Product Name";
        productsSheet.Cell("B1").Value = "Quantity Sold";
        productsSheet.Cell("C1").Value = "Revenue";
        productsSheet.Range("A1:C1").Style.Fill.BackgroundColor = XLColor.LightBlue;
        productsSheet.Range("A1:C1").Style.Font.Bold = true;
        
        var row = 2;
        foreach (var product in TopProducts)
        {
            productsSheet.Cell(row, 1).Value = product.ProductName;
            productsSheet.Cell(row, 2).Value = product.QuantitySold;
            productsSheet.Cell(row, 3).Value = product.Revenue;
            productsSheet.Cell(row, 3).Style.NumberFormat.Format = "£#,##0.00";
            row++;
        }
        
        productsSheet.Columns().AdjustToContents();
        
        // Employee Sales sheet
        var employeeSheet = workbook.Worksheets.Add("Employee Sales");
        employeeSheet.Cell("A1").Value = "Employee";
        employeeSheet.Cell("B1").Value = "Transactions";
        employeeSheet.Cell("C1").Value = "Total Sales";
        employeeSheet.Range("A1:C1").Style.Fill.BackgroundColor = XLColor.LightBlue;
        employeeSheet.Range("A1:C1").Style.Font.Bold = true;
        
        row = 2;
        foreach (var emp in EmployeeSales)
        {
            employeeSheet.Cell(row, 1).Value = emp.EmployeeName;
            employeeSheet.Cell(row, 2).Value = emp.TransactionCount;
            employeeSheet.Cell(row, 3).Value = emp.TotalSales;
            employeeSheet.Cell(row, 3).Style.NumberFormat.Format = "£#,##0.00";
            row++;
        }
        
        employeeSheet.Columns().AdjustToContents();
        
        workbook.SaveAs(filePath);
        Debug.WriteLine($"[Reports] Excel generated successfully");
    }
}

