using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using POSSystem.Data.Interfaces;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.ViewModels;


/// <summary>
/// ViewModel for Inventory Management with product CRUD operations.
/// </summary>
public partial class InventoryViewModel : ObservableObject
{
    private readonly IDataService _dataService;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private ObservableCollection<string> _categories = new() { "All" };

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showProductDialog;

    [ObservableProperty]
    private bool _isEditMode;

    // Product dialog fields
    [ObservableProperty]
    private string _dialogProductName = string.Empty;

    [ObservableProperty]
    private decimal _dialogPrice;

    [ObservableProperty]
    private decimal _dialogCost;

    [ObservableProperty]
    private string _dialogCategory = string.Empty;

    [ObservableProperty]
    private string _dialogSku = string.Empty;

    [ObservableProperty]
    private string _dialogBarcode = string.Empty;

    [ObservableProperty]
    private int _dialogStock;

    [ObservableProperty]
    private decimal _dialogTaxRate = 0.14m;

    [ObservableProperty]
    private int _dialogMinStock = 5;

    [ObservableProperty]
    private ObservableCollection<Product> _lowStockProducts = new();
    
    [ObservableProperty]
    private int _lowStockCount;
    
    [ObservableProperty]
    private bool _showLowStockOnly;

    private Guid? _editingProductId;

    private bool _selectAll;
    public bool SelectAll
    {
        get => _selectAll;
        set
        {
            if (SetProperty(ref _selectAll, value))
            {
                foreach (var product in Products)
                    product.IsSelected = value;
                OnPropertyChanged(nameof(Products));
            }
        }
    }
    
    partial void OnShowLowStockOnlyChanged(bool value) => _ = LoadProductsAsync();

    public InventoryViewModel(IDataService dataService)
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
        await LoadProductsAsync();
        await LoadCategoriesAsync();
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        try
        {
            var allProducts = (await _dataService.GetAllProductsAsync()).ToList();
            
            // Track low stock products
            var lowStock = allProducts.Where(p => p.StockQuantity <= p.MinStockLevel).ToList();
            LowStockProducts = new ObservableCollection<Product>(lowStock);
            LowStockCount = lowStock.Count;
            
            // Apply filters
            var products = allProducts.AsEnumerable();
            
            if (ShowLowStockOnly)
            {
                products = lowStock;
            }
            
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.ToLower();
                products = products.Where(p => 
                    p.Name.ToLower().Contains(term) ||
                    (p.Sku?.ToLower().Contains(term) ?? false) ||
                    (p.Barcode?.Contains(term) ?? false));
            }

            if (SelectedCategory != "All")
            {
                products = products.Where(p => p.Category == SelectedCategory);
            }

            Products = new ObservableCollection<Product>(products);
            Debug.WriteLine($"[Inventory] Loaded {Products.Count} products ({LowStockCount} low stock)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Inventory] Load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _dataService.GetCategoriesAsync();
        Categories = new ObservableCollection<string>(new[] { "All" }.Concat(categories));
    }

    partial void OnSearchTextChanged(string value) => _ = LoadProductsAsync();
    partial void OnSelectedCategoryChanged(string value) => _ = LoadProductsAsync();

    [RelayCommand]
    private void ShowAddProduct()
    {
        IsEditMode = false;
        _editingProductId = null;
        ClearDialogFields();
        ShowProductDialog = true;
    }

    [RelayCommand]
    private async Task ImportCsvAsync()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select CSV File to Import"
            };

            if (dialog.ShowDialog() != true) return;

            IsLoading = true;
            var imported = 0;
            var skipped = 0;
            var lines = await File.ReadAllLinesAsync(dialog.FileName);

            // Skip header row if exists
            var startIndex = lines.Length > 0 && lines[0].ToLower().Contains("name") ? 1 : 0;

            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var product = ParseCsvLine(line);
                if (product != null)
                {
                    await _dataService.AddProductAsync(product);
                    imported++;
                }
                else
                {
                    skipped++;
                }
            }

            await LoadProductsAsync();
            await LoadCategoriesAsync();
            
            MessageBox.Show($"Import complete!\n\nImported: {imported} products\nSkipped: {skipped} rows", 
                "CSV Import", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Import] CSV Error: {ex.Message}");
            MessageBox.Show($"Error importing CSV:\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select Excel/CSV File to Import"
            };

            if (dialog.ShowDialog() != true) return;

            // For Excel files, convert to CSV first using minimal parsing
            // Note: Full Excel support requires EPPlus or ClosedXML NuGet package
            var extension = Path.GetExtension(dialog.FileName).ToLower();
            
            if (extension == ".csv")
            {
                // Redirect to CSV import
                await ImportCsvAsync();
                return;
            }
            
            // Basic Excel import message for now
            MessageBox.Show(
                "For Excel (.xlsx/.xls) files, please save them as CSV first:\n\n" +
                "1. Open your Excel file\n" +
                "2. File → Save As\n" +
                "3. Choose 'CSV (Comma delimited) (*.csv)'\n" +
                "4. Use the 'Import CSV' button\n\n" +
                "Expected CSV columns:\n" +
                "Name, Price, Cost, Category, SKU, Barcode, Stock, TaxRate",
                "Excel Import", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Import] Excel Error: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Product? ParseCsvLine(string line)
    {
        try
        {
            // Handle quoted fields and commas within quotes
            var fields = ParseCsvFields(line);
            if (fields.Count < 1 || string.IsNullOrWhiteSpace(fields[0])) return null;

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = fields[0].Trim(),
                Price = fields.Count > 1 ? ParseDecimal(fields[1]) : 0m,
                Cost = fields.Count > 2 ? ParseDecimal(fields[2]) : 0m,
                Category = fields.Count > 3 ? fields[3].Trim() : "Uncategorized",
                Sku = fields.Count > 4 ? fields[4].Trim() : "",
                Barcode = fields.Count > 5 ? fields[5].Trim() : "",
                StockQuantity = fields.Count > 6 ? ParseInt(fields[6]) : 0,
                TaxRate = fields.Count > 7 ? ParseDecimal(fields[7]) : 0.14m,
                IsActive = true
            };

            return product;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Import] Parse error: {ex.Message} - Line: {line}");
            return null;
        }
    }

    private List<string> ParseCsvFields(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var field = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(field);
                field = "";
            }
            else
            {
                field += c;
            }
        }
        fields.Add(field);
        return fields;
    }

    private decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;
        value = value.Trim().Replace("$", "").Replace("£", "").Replace("€", "").Replace("%", "");
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0m;
    }

    private int ParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        value = value.Trim();
        return int.TryParse(value, out var result) ? result : 0;
    }

    [RelayCommand]
    private void ShowEditProduct()
    {
        if (SelectedProduct == null) return;

        IsEditMode = true;
        _editingProductId = SelectedProduct.Id;
        
        DialogProductName = SelectedProduct.Name;
        DialogPrice = SelectedProduct.Price;
        DialogCost = SelectedProduct.Cost;
        DialogCategory = SelectedProduct.Category ?? "";
        DialogSku = SelectedProduct.Sku ?? "";
        DialogBarcode = SelectedProduct.Barcode ?? "";
        DialogStock = SelectedProduct.StockQuantity;
        DialogTaxRate = SelectedProduct.TaxRate;
        DialogMinStock = SelectedProduct.MinStockLevel;
        
        ShowProductDialog = true;
    }

    [RelayCommand]
    private async Task SaveProductAsync()
    {
        if (string.IsNullOrWhiteSpace(DialogProductName))
        {
            MessageBox.Show("Product name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (IsEditMode && _editingProductId.HasValue)
            {
                // Update existing
                var product = await _dataService.GetProductByIdAsync(_editingProductId.Value);
                if (product != null)
                {
                    product.Name = DialogProductName;
                    product.Price = DialogPrice;
                    product.Cost = DialogCost;
                    product.Category = string.IsNullOrWhiteSpace(DialogCategory) ? null : DialogCategory;
                    product.Sku = string.IsNullOrWhiteSpace(DialogSku) ? null : DialogSku;
                    product.Barcode = string.IsNullOrWhiteSpace(DialogBarcode) ? null : DialogBarcode;
                    product.StockQuantity = DialogStock;
                    product.TaxRate = DialogTaxRate;
                    product.MinStockLevel = DialogMinStock;
                    product.LastUpdatedBy = UpdateSource.Desktop;

                    await _dataService.UpdateProductAsync(product);
                    Debug.WriteLine($"[Inventory] Updated: {product.Name}");
                }
            }
            else
            {
                // Add new
                var product = new Product
                {
                    Name = DialogProductName,
                    Price = DialogPrice,
                    Cost = DialogCost,
                    Category = string.IsNullOrWhiteSpace(DialogCategory) ? null : DialogCategory,
                    Sku = string.IsNullOrWhiteSpace(DialogSku) ? null : DialogSku,
                    Barcode = string.IsNullOrWhiteSpace(DialogBarcode) ? null : DialogBarcode,
                    StockQuantity = DialogStock,
                    TaxRate = DialogTaxRate,
                    MinStockLevel = DialogMinStock,
                    LastUpdatedBy = UpdateSource.Desktop
                };

                await _dataService.AddProductAsync(product);
                Debug.WriteLine($"[Inventory] Added: {product.Name}");
            }

            ShowProductDialog = false;
            await LoadProductsAsync();
            await LoadCategoriesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving product: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteProductAsync()
    {
        if (SelectedProduct == null) return;

        var result = MessageBox.Show(
            $"Delete '{SelectedProduct.Name}'?\n\nThis action cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            await _dataService.DeleteProductAsync(SelectedProduct.Id);
            Debug.WriteLine($"[Inventory] Deleted: {SelectedProduct.Name}");
            await LoadProductsAsync();
            await LoadCategoriesAsync(); // Refresh categories after deletion
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selected = Products.Where(p => p.IsSelected).ToList();
        if (!selected.Any())
        {
            MessageBox.Show("No products selected.", "Delete Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Delete {selected.Count} selected product(s)?\n\nThis action cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            foreach (var product in selected)
            {
                await _dataService.DeleteProductAsync(product.Id);
                Debug.WriteLine($"[Inventory] Deleted: {product.Name}");
            }
            SelectAll = false;
            await LoadProductsAsync();
            await LoadCategoriesAsync(); // Refresh categories after deletion
        }
    }

    [RelayCommand]
    private void CancelDialog()
    {
        ShowProductDialog = false;
        ClearDialogFields();
    }

    private void ClearDialogFields()
    {
        DialogProductName = string.Empty;
        DialogPrice = 0;
        DialogCost = 0;
        DialogCategory = string.Empty;
        DialogSku = string.Empty;
        DialogBarcode = string.Empty;
        DialogStock = 0;
        DialogTaxRate = 0.14m;
        DialogMinStock = 5;
    }

    /// <summary>
    /// Called by DashboardViewModel when a sale is made to decrement stock.
    /// </summary>
    public async Task DecrementStockAsync(Guid productId, int quantity)
    {
        var product = await _dataService.GetProductByIdAsync(productId);
        if (product != null)
        {
            var previousStock = product.StockQuantity;
            product.StockQuantity = Math.Max(0, product.StockQuantity - quantity);
            product.LastUpdatedBy = UpdateSource.Desktop;
            await _dataService.UpdateProductAsync(product);
            
            Debug.WriteLine($"[Inventory] Stock decremented: {product.Name} now has {product.StockQuantity}");

            // Check if this decrement caused low stock condition
            var wasAboveMinimum = previousStock > product.MinStockLevel;
            var isNowBelowMinimum = product.StockQuantity <= product.MinStockLevel;
            
            if (wasAboveMinimum && isNowBelowMinimum)
            {
                Debug.WriteLine($"[Inventory] ⚠️ Low stock warning: {product.Name}");
                await SendLowStockAlertAsync(product);
            }
            
            // Check for out of stock
            if (product.StockQuantity == 0 && previousStock > 0)
            {
                Debug.WriteLine($"[Inventory] 🚨 OUT OF STOCK: {product.Name}");
                await SendOutOfStockAlertAsync(product);
            }
        }
    }
    
    private async Task SendLowStockAlertAsync(Product product)
    {
        try
        {
            var emailService = App.Current.Services.GetService<IEmailService>();
            if (emailService == null) return;
            
            var subject = $"⚠️ Low Stock Alert: {product.Name}";
            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #F59E0B;'>⚠️ Low Stock Warning</h2>
    <p>The following product is running low on stock:</p>
    
    <table style='border-collapse: collapse; margin: 20px 0;'>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Product</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>{product.Name}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>SKU</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>{product.Sku ?? "N/A"}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Current Stock</td>
            <td style='padding: 10px; border: 1px solid #ddd; color: #F59E0B; font-weight: bold;'>{product.StockQuantity}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Minimum Level</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>{product.MinStockLevel}</td>
        </tr>
    </table>
    
    <p>Please restock this item soon.</p>
    <p style='color: #666;'>This is an automated notification from POS System.</p>
</body>
</html>";

            await emailService.SendAdminNotificationAsync(subject, body);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Inventory] Failed to send low stock alert: {ex.Message}");
        }
    }
    
    private async Task SendOutOfStockAlertAsync(Product product)
    {
        try
        {
            var emailService = App.Current.Services.GetService<IEmailService>();
            if (emailService == null) return;
            
            var subject = $"🚨 OUT OF STOCK: {product.Name}";
            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #DC2626;'>🚨 Out of Stock Alert</h2>
    <p>The following product is now <strong>OUT OF STOCK</strong>:</p>
    
    <table style='border-collapse: collapse; margin: 20px 0;'>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Product</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>{product.Name}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>SKU</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>{product.Sku ?? "N/A"}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Category</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>{product.Category ?? "Uncategorized"}</td>
        </tr>
    </table>
    
    <p style='color: #DC2626; font-weight: bold;'>Immediate restocking required!</p>
    <p style='color: #666;'>This is an automated notification from POS System.</p>
</body>
</html>";

            await emailService.SendAdminNotificationAsync(subject, body);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Inventory] Failed to send out of stock alert: {ex.Message}");
        }
    }
}

