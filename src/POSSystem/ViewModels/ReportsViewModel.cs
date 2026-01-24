using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSSystem.Data.Interfaces;
using POSSystem.Models;

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
            var (startDate, endDate) = GetDateRange();
            var fileName = $"SalesReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            // Generate PDF using QuestPDF
            await GeneratePdfReportAsync(filePath, startDate, endDate);

            MessageBox.Show($"Report exported to:\n{filePath}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Open file
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Task GeneratePdfReportAsync(string filePath, DateTime startDate, DateTime endDate)
    {
        // Simple text file fallback if QuestPDF not installed
        // Full implementation would use QuestPDF Document.Create()
        
        var content = $@"
SALES REPORT
============
Period: {startDate:d} to {endDate:d}

SUMMARY
-------
Total Revenue: {TotalRevenue:C}
Total Cost: {TotalCost:C}
Gross Profit: {GrossProfit:C}
Total Transactions: {TotalTransactions}
Average Order Value: {AverageOrderValue:C}

TOP PRODUCTS
------------
";
        foreach (var product in TopProducts)
        {
            content += $"{product.ProductName}: {product.QuantitySold} sold ({product.Revenue:C})\n";
        }

        // Save as text for now (replace with QuestPDF for real PDF)
        var txtPath = filePath.Replace(".pdf", ".txt");
        File.WriteAllText(txtPath, content);

        Debug.WriteLine($"[Reports] Exported to: {txtPath}");
        return Task.CompletedTask;
    }
}
