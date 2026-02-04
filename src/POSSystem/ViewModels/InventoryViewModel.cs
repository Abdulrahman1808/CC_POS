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

    // New inventory dialog fields
    [ObservableProperty]
    private string _dialogProductType = string.Empty;

    [ObservableProperty]
    private string _dialogFlavor = string.Empty;

    [ObservableProperty]
    private string _dialogWeight = string.Empty;

    [ObservableProperty]
    private string _dialogLocation = string.Empty;

    [ObservableProperty]
    private int _dialogRetailQuantity;

    [ObservableProperty]
    private int _dialogCartonCount;

    [ObservableProperty]
    private int _dialogUnitsPerCarton;

    [ObservableProperty]
    private decimal _dialogRetailSalePrice;

    [ObservableProperty]
    private decimal _dialogWholesaleSalePrice;

    [ObservableProperty]
    private decimal _dialogWholesaleSupplierPrice;

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

            var extension = Path.GetExtension(dialog.FileName).ToLower();
            
            if (extension == ".csv")
            {
                await ImportCsvAsync();
                return;
            }

            IsLoading = true;
            var imported = 0;
            var skipped = 0;

            await Task.Run(async () =>
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook(dialog.FileName);
                var worksheet = workbook.Worksheets.First();
                var rows = worksheet.RangeUsed()?.RowsUsed().ToList();
                
                if (rows == null || rows.Count == 0)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        MessageBox.Show("Excel file is empty.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }

                // Try to detect column mappings from header row
                var columnMap = DetectColumnMapping(rows[0]);
                var startRow = columnMap.Any() ? 1 : 0; // Skip header if detected

                for (int i = startRow; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var product = ParseExcelRow(row, columnMap);
                    
                    if (product != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await _dataService.AddProductAsync(product);
                        });
                        imported++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
            });

            await LoadProductsAsync();
            await LoadCategoriesAsync();
            
            MessageBox.Show($"Excel Import Complete!\n\nImported: {imported} products\nSkipped: {skipped} rows", 
                "Excel Import", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Import] Excel Error: {ex.Message}");
            MessageBox.Show($"Error importing Excel:\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Dictionary<string, int> DetectColumnMapping(ClosedXML.Excel.IXLRangeRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var cells = headerRow.CellsUsed().ToList();
        
        for (int i = 0; i < cells.Count; i++)
        {
            var header = cells[i].GetString().Trim().ToLower();
            var colIndex = cells[i].Address.ColumnNumber;
            
            // Core fields
            if (header == "id")
                map["Id"] = colIndex;
            else if (header == "name" || header.Contains("اسم"))
                map["Name"] = colIndex;
            else if (header == "category" || header.Contains("فئة"))
                map["Category"] = colIndex;
            else if (header == "quantity" || header == "qty")
                map["Quantity"] = colIndex;
            else if (header == "retail_quantity" || header.Contains("retail_qty"))
                map["RetailQuantity"] = colIndex;
            else if (header == "location" || header.Contains("موقع"))
                map["Location"] = colIndex;
            else if (header == "barcode" || header.Contains("باركود"))
                map["Barcode"] = colIndex;
            else if (header == "type" || header == "product_type")
                map["ProductType"] = colIndex;
            else if (header == "weight" || header.Contains("وزن"))
                map["Weight"] = colIndex;
            else if (header == "carton_count" || header.Contains("carton"))
                map["CartonCount"] = colIndex;
            else if (header == "units_per_carton" || header.Contains("units_per"))
                map["UnitsPerCarton"] = colIndex;
            else if (header == "flavor" || header.Contains("نكهة"))
                map["Flavor"] = colIndex;
            else if (header == "carton_fraction")
                map["CartonFraction"] = colIndex;
            else if (header == "unit_type")
                map["UnitType"] = colIndex;
            else if (header == "wholesale_supplier_price" || header.Contains("supplier"))
                map["WholesaleSupplierPrice"] = colIndex;
            else if (header == "wholesale_sale_price" || header.Contains("wholesale_sale"))
                map["WholesaleSalePrice"] = colIndex;
            else if (header == "retail_sale_price" || header.Contains("retail_sale"))
                map["RetailSalePrice"] = colIndex;
            else if (header == "extra_retail_quantity")
                map["ExtraRetailQuantity"] = colIndex;
            else if (header == "min_quantity" || header.Contains("min_stock"))
                map["MinQuantity"] = colIndex;
            else if (header == "image" || header.Contains("صورة"))
                map["ImagePath"] = colIndex;
            // Fallback for price/cost/sku/tax
            else if (header.Contains("price") || header.Contains("سعر"))
                map["Price"] = colIndex;
            else if (header.Contains("cost") || header.Contains("تكلفة"))
                map["Cost"] = colIndex;
            else if (header.Contains("sku") || header.Contains("code"))
                map["SKU"] = colIndex;
            else if (header.Contains("tax") || header.Contains("ضريبة"))
                map["TaxRate"] = colIndex;
        }

        return map;
    }


    private Product? ParseExcelRow(ClosedXML.Excel.IXLRangeRow row, Dictionary<string, int> columnMap)
    {
        try
        {
            string GetCellValue(string key, int defaultCol = 0)
            {
                if (defaultCol == 0 && !columnMap.ContainsKey(key)) return "";
                var col = columnMap.TryGetValue(key, out var c) ? c : defaultCol;
                if (col == 0) return "";
                return row.Cell(col).GetString().Trim();
            }

            var name = GetCellValue("Name", 2); // Column 2 is 'name' in CSV
            if (string.IsNullOrWhiteSpace(name)) return null;

            return new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                Category = GetCellValue("Category").Length > 0 ? GetCellValue("Category") : "Uncategorized",
                StockQuantity = ParseInt(GetCellValue("Quantity")),
                RetailQuantity = ParseInt(GetCellValue("RetailQuantity")),
                Location = GetCellValue("Location"),
                Barcode = GetCellValue("Barcode"),
                ProductType = GetCellValue("ProductType"),
                Weight = GetCellValue("Weight"),
                CartonCount = ParseInt(GetCellValue("CartonCount")),
                UnitsPerCarton = ParseInt(GetCellValue("UnitsPerCarton")),
                Flavor = GetCellValue("Flavor"),
                CartonFraction = ParseDecimal(GetCellValue("CartonFraction")),
                UnitType = GetCellValue("UnitType"),
                WholesaleSupplierPrice = ParseDecimal(GetCellValue("WholesaleSupplierPrice")),
                WholesaleSalePrice = ParseDecimal(GetCellValue("WholesaleSalePrice")),
                RetailSalePrice = ParseDecimal(GetCellValue("RetailSalePrice")),
                ExtraRetailQuantity = ParseInt(GetCellValue("ExtraRetailQuantity")),
                MinStockLevel = ParseInt(GetCellValue("MinQuantity")),
                ImagePath = GetCellValue("ImagePath"),
                // Use RetailSalePrice as default Price if available, else WholesaleSalePrice
                Price = ParseDecimal(GetCellValue("RetailSalePrice")) > 0 
                    ? ParseDecimal(GetCellValue("RetailSalePrice")) 
                    : ParseDecimal(GetCellValue("WholesaleSalePrice")),
                Cost = ParseDecimal(GetCellValue("WholesaleSupplierPrice")),
                TaxRate = 0.14m,
                IsActive = true
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Import] Excel row parse error: {ex.Message}");
            return null;
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
        // New fields
        DialogProductType = SelectedProduct.ProductType ?? "";
        DialogFlavor = SelectedProduct.Flavor ?? "";
        DialogWeight = SelectedProduct.Weight ?? "";
        DialogLocation = SelectedProduct.Location ?? "";
        DialogRetailQuantity = SelectedProduct.RetailQuantity;
        DialogCartonCount = SelectedProduct.CartonCount;
        DialogUnitsPerCarton = SelectedProduct.UnitsPerCarton;
        DialogRetailSalePrice = SelectedProduct.RetailSalePrice;
        DialogWholesaleSalePrice = SelectedProduct.WholesaleSalePrice;
        DialogWholesaleSupplierPrice = SelectedProduct.WholesaleSupplierPrice;
        
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
                    // New fields
                    product.ProductType = string.IsNullOrWhiteSpace(DialogProductType) ? null : DialogProductType;
                    product.Flavor = string.IsNullOrWhiteSpace(DialogFlavor) ? null : DialogFlavor;
                    product.Weight = string.IsNullOrWhiteSpace(DialogWeight) ? null : DialogWeight;
                    product.Location = string.IsNullOrWhiteSpace(DialogLocation) ? null : DialogLocation;
                    product.RetailQuantity = DialogRetailQuantity;
                    product.CartonCount = DialogCartonCount;
                    product.UnitsPerCarton = DialogUnitsPerCarton;
                    product.RetailSalePrice = DialogRetailSalePrice;
                    product.WholesaleSalePrice = DialogWholesaleSalePrice;
                    product.WholesaleSupplierPrice = DialogWholesaleSupplierPrice;
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
                    ProductType = string.IsNullOrWhiteSpace(DialogProductType) ? null : DialogProductType,
                    Flavor = string.IsNullOrWhiteSpace(DialogFlavor) ? null : DialogFlavor,
                    Weight = string.IsNullOrWhiteSpace(DialogWeight) ? null : DialogWeight,
                    Location = string.IsNullOrWhiteSpace(DialogLocation) ? null : DialogLocation,
                    RetailQuantity = DialogRetailQuantity,
                    CartonCount = DialogCartonCount,
                    UnitsPerCarton = DialogUnitsPerCarton,
                    RetailSalePrice = DialogRetailSalePrice,
                    WholesaleSalePrice = DialogWholesaleSalePrice,
                    WholesaleSupplierPrice = DialogWholesaleSupplierPrice,
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
        // New fields
        DialogProductType = string.Empty;
        DialogFlavor = string.Empty;
        DialogWeight = string.Empty;
        DialogLocation = string.Empty;
        DialogRetailQuantity = 0;
        DialogCartonCount = 0;
        DialogUnitsPerCarton = 0;
        DialogRetailSalePrice = 0;
        DialogWholesaleSalePrice = 0;
        DialogWholesaleSupplierPrice = 0;
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

