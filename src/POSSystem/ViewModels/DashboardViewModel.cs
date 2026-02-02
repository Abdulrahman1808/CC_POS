using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Data;
using POSSystem.Data.Interfaces;
using POSSystem.Models;
using POSSystem.Services;
using POSSystem.Services.Interfaces;

namespace POSSystem.ViewModels;

/// <summary>
/// ViewModel for the main POS dashboard using CommunityToolkit.Mvvm.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly ISyncService _syncService;
    private readonly IUpdateService _updateService;
    private readonly ILicenseManager? _licenseManager;
    private readonly ISystemHealthService? _healthService;

    #region Observable Properties

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private bool _isOnline;

    [ObservableProperty]
    private int _pendingSyncCount;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string? _newVersion;

    [ObservableProperty]
    private decimal _subTotal;

    [ObservableProperty]
    private decimal _taxAmount;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private decimal _dailySales;

    [ObservableProperty]
    private int _transactionCount;

    [ObservableProperty]
    private bool _isCheckoutVisible;

    [ObservableProperty]
    private string _selectedPaymentMethod = "Cash";

    [ObservableProperty]
    private decimal _amountTendered;

    [ObservableProperty]
    private decimal _changeAmount;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _fawryReferenceNumber = string.Empty;

    [ObservableProperty]
    private bool _isFawryPending;

    /// <summary>
    /// Customer phone for Card/Mobile receipts.
    /// </summary>
    [ObservableProperty]
    private string _customerPhone = string.Empty;

    /// <summary>
    /// Show phone input for Card and Mobile payments.
    /// </summary>
    public bool ShowPhoneField => SelectedPaymentMethod == "Card" || SelectedPaymentMethod == "Mobile";

    // ===== DEVELOPER MODE PROPERTIES =====
    
    /// <summary>
    /// Whether the app is in developer mode (DevSecret2026 activated).
    /// </summary>
    [ObservableProperty]
    private bool _isDeveloperMode;
    
    /// <summary>
    /// Machine ID for display in developer overlay.
    /// </summary>
    [ObservableProperty]
    private string _machineId = string.Empty;

#if DEBUG
    public bool IsDebugMode => true;
#else
    public bool IsDebugMode => false;
#endif

    #endregion

    #region Collections

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<CartItem> CartItems { get; } = new();
    public ObservableCollection<Transaction> RecentTransactions { get; } = new();
    public ObservableCollection<string> PaymentMethods { get; } = new() { "Cash", "Card", "Mobile" };

    #endregion

    public DashboardViewModel(
        IDataService dataService,
        ISyncService syncService,
        IUpdateService updateService)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        
        // Try to get license and health services from DI
        try
        {
            _licenseManager = App.Current.Services.GetService<ILicenseManager>();
            _healthService = App.Current.Services.GetService<ISystemHealthService>();
            
            // Initialize developer mode properties
            if (_licenseManager != null)
            {
                IsDeveloperMode = _licenseManager.IsDeveloperMode;
                MachineId = _licenseManager.MachineId;
                
                // Override IsOnline if in developer mode
                if (IsDeveloperMode)
                {
                    IsOnline = true; // Developer mode always shows as online
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Dashboard] Could not load license/health services: {ex.Message}");
        }

        // Subscribe to events
        _syncService.SyncStatusChanged += OnSyncStatusChanged;
        _updateService.UpdateAvailable += OnUpdateAvailable;

        // Initialize
        Categories.Add("All");
    }

    #region Navigation

    /// <summary>
    /// Event raised when navigation to a different view is requested.
    /// </summary>
    public event EventHandler<string>? NavigationRequested;

    [RelayCommand]
    private void NavigateToInventory() => NavigationRequested?.Invoke(this, "Inventory");

    [RelayCommand]
    private void NavigateToReports() => NavigationRequested?.Invoke(this, "Reports");

    [RelayCommand]
    private void NavigateToSettings() => NavigationRequested?.Invoke(this, "Settings");

    #endregion

    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
        await LoadProductsAsync();
        await LoadRecentTransactionsAsync();
        await LoadDailySalesAsync();
        await _updateService.CheckForUpdatesAsync();
    }

    #region Commands

    [RelayCommand]
    private async Task AddToCartAsync(object? parameter)
    {
        var product = parameter as Product ?? SelectedProduct;
        if (product == null) return;

        var existing = CartItems.FirstOrDefault(c => c.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity++;
            existing.OnPropertyChanged(nameof(CartItem.Quantity));
            existing.OnPropertyChanged(nameof(CartItem.LineTotal));
        }
        else
        {
            CartItems.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                TaxRate = product.TaxRate, // Per-product tax rate
                Quantity = 1
            });
        }

        UpdateTotals();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void RemoveFromCart(CartItem? item)
    {
        if (item == null) return;

        if (item.Quantity > 1)
        {
            item.Quantity--;
            item.OnPropertyChanged(nameof(CartItem.Quantity));
            item.OnPropertyChanged(nameof(CartItem.LineTotal));
        }
        else
        {
            CartItems.Remove(item);
        }

        UpdateTotals();
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        UpdateTotals();
    }

    [RelayCommand]
    private void ShowCheckout()
    {
        if (!CartItems.Any()) return;
        
        AmountTendered = TotalAmount;
        ChangeAmount = 0;
        IsCheckoutVisible = true;
    }

    [RelayCommand]
    private void HideCheckout()
    {
        IsCheckoutVisible = false;
    }

    [RelayCommand]
    private async Task ProcessPaymentAsync()
    {
        if (!CartItems.Any() || IsProcessing) return;

        IsProcessing = true;

        try
        {
            // Handle different payment methods
            switch (SelectedPaymentMethod)
            {
                case "Cash":
                    await ProcessCashPaymentAsync();
                    break;
                case "Card":
                    await ProcessCardPaymentAsync();
                    break;
                case "Mobile":
                    await ProcessMobilePaymentAsync();
                    break;
            }
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task ProcessCashPaymentAsync()
    {
        Debug.WriteLine($"[Payment] Cash: Starting payment for {CartItems.Count} items, Total: {TotalAmount}");
        
        var transaction = CreateTransaction();
        Debug.WriteLine($"[Payment] Cash: Transaction created with {transaction.Items?.Count ?? 0} items");
        
        var success = await _dataService.CreateTransactionAsync(transaction);
        Debug.WriteLine($"[Payment] Cash: CreateTransactionAsync returned {success}");
        
        if (success)
        {
            // Calculate change
            ChangeAmount = AmountTendered - TotalAmount;
            
#if DEBUG
            // Mock cash drawer open
            OpenCashDrawer();
#endif
            
            // Show success message with change due
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                System.Windows.MessageBox.Show(
                    $"Sale Complete!\n\nTotal: {TotalAmount:C2}\nTendered: {AmountTendered:C2}\n\n💵 Change Due: {ChangeAmount:C2}",
                    "Cash Payment Success",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            });
            
            CompleteCheckout();
        }
        else
        {
            Debug.WriteLine("[Payment] Cash: FAILED - CreateTransactionAsync returned false");
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                System.Windows.MessageBox.Show(
                    "Failed to save transaction. Please try again.",
                    "Payment Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            });
        }
    }

    private async Task ProcessCardPaymentAsync()
    {
        // Simulate bank authorization delay
        Debug.WriteLine("[Payment] Card: Authorizing with bank...");
        await Task.Delay(2500); // Simulate terminal delay
        Debug.WriteLine("[Payment] Card: Authorization approved");
        
        var transaction = CreateTransaction();
        
        if (await _dataService.CreateTransactionAsync(transaction))
        {
            ChangeAmount = 0; // No change for card
            
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                System.Windows.MessageBox.Show(
                    $"Card Payment Approved!\n\nTotal: {TotalAmount:C2}",
                    "Payment Success",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            });
            
            CompleteCheckout();
        }
    }

    private async Task ProcessMobilePaymentAsync()
    {
        // Generate Fawry reference number
        FawryReferenceNumber = GenerateFawryReference();
        IsFawryPending = true;
        
        Debug.WriteLine($"[Payment] Fawry Reference: {FawryReferenceNumber}");
        
        var transaction = CreateTransaction();
        transaction.PaymentReference = FawryReferenceNumber;
        
        if (await _dataService.CreateTransactionAsync(transaction))
        {
            // Show Fawry reference for customer to pay
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                System.Windows.MessageBox.Show(
                    $"Fawry Payment Reference Generated!\n\nTotal: {TotalAmount:C2}\n\n📱 Reference: {FawryReferenceNumber}\n\nCustomer should pay using this code at any Fawry outlet.",
                    "Mobile Payment",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            });
            
            IsFawryPending = false;
            CompleteCheckout();
        }
    }

    private Transaction CreateTransaction()
    {
        var transactionId = Guid.NewGuid();
        
        return new Transaction
        {
            Id = transactionId,
            PaymentMethod = SelectedPaymentMethod,
            CustomerPhone = CustomerPhone, // For Card/Mobile receipts
            Items = CartItems.Select(c => new TransactionItem
            {
                TransactionId = transactionId, // Must set for [Required] validation
                ProductId = c.ProductId,
                ProductName = c.ProductName,
                UnitPrice = c.UnitPrice,
                Quantity = c.Quantity
            }).ToList()
        };
    }

    private void CompleteCheckout()
    {
        // Decrement stock for sold items
        _ = DecrementStockAsync();
        
        // Clear cart and reset UI (done before ClearCart to properly decrement stock)
        var itemsToProcess = CartItems.ToList(); // Copy for stock processing
        
        ClearCart(); // This resets SubTotal, TaxAmount, TotalAmount to 0
        
        // Reset all payment-related values
        AmountTendered = 0;
        ChangeAmount = 0;
        FawryReferenceNumber = string.Empty;
        CustomerPhone = string.Empty;
        IsFawryPending = false;
        SelectedPaymentMethod = "Cash";
        
        // Hide checkout panel
        IsCheckoutVisible = false;
        
        // Refresh data
        _ = LoadRecentTransactionsAsync();
        _ = LoadDailySalesAsync();
        _ = LoadProductsAsync(); // Refresh to show updated stock
    }

    private async Task DecrementStockAsync()
    {
        try
        {
            foreach (var item in CartItems)
            {
                var product = Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    product.StockQuantity = Math.Max(0, product.StockQuantity - item.Quantity);
                    await _dataService.UpdateProductAsync(product);
                    
                    // Check for low stock warning
                    if (product.StockQuantity <= product.MinStockLevel)
                    {
                        Debug.WriteLine($"[Stock] LOW STOCK WARNING: {product.Name} ({product.StockQuantity} remaining)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Stock] Error decrementing stock: {ex.Message}");
        }
    }

#if DEBUG
    private void OpenCashDrawer()
    {
        Debug.WriteLine("[Cash Drawer] OPEN - Mock cash drawer triggered");
    }
#endif

    private static string GenerateFawryReference()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var part1 = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        var part2 = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        return $"FWY-{part1}-{part2}";
    }

    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        await LoadProductsAsync();
        await LoadRecentTransactionsAsync();
        await LoadDailySalesAsync();
    }

    [RelayCommand]
    private async Task ApplyUpdateAsync()
    {
        await _updateService.ApplyUpdateAsync();
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        await _syncService.SyncAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await SearchProductsAsync();
    }

    [RelayCommand]
    private async Task FilterByCategoryAsync(string? category)
    {
        if (category != null)
        {
            SelectedCategory = category;
            await FilterProductsAsync();
        }
    }

#if DEBUG
    /// <summary>
    /// Creates a random test transaction for stress-testing sync.
    /// Only available in DEBUG builds.
    /// </summary>
    [RelayCommand]
    private async Task CreateTestSaleAsync()
    {
        Debug.WriteLine("[Test] Creating test transaction...");
        
        var transaction = await DataSeeder.CreateTestTransactionAsync(_dataService);
        
        if (transaction != null)
        {
            await LoadRecentTransactionsAsync();
            await LoadDailySalesAsync();
            Debug.WriteLine($"[Test] ✓ Test transaction created! Pending sync count will update shortly.");
        }
    }
#endif

    #endregion

    #region Developer Mode Commands

    /// <summary>
    /// Runs system health diagnostics (Database, Supabase, Printer, License).
    /// </summary>
    [RelayCommand]
    private async Task RunHealthCheckAsync()
    {
        if (_healthService == null)
        {
            Debug.WriteLine("[DevMode] Health service not available");
            return;
        }
        
        Debug.WriteLine("[DevMode] Running system health check...");
        var report = await _healthService.RunHealthCheckAsync();
        
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            System.Windows.MessageBox.Show(
                report.ToString(),
                "System Health Check",
                System.Windows.MessageBoxButton.OK,
                report.IsHealthy ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Warning);
        });
    }

    /// <summary>
    /// Forces a sync with the cloud.
    /// </summary>
    [RelayCommand]
    private async Task ForceSyncAsync()
    {
        Debug.WriteLine("[DevMode] Forcing sync...");
        await _syncService.SyncAsync();
        Debug.WriteLine("[DevMode] Sync complete");
    }

    /// <summary>
    /// Clears all test data (transactions and sync records).
    /// </summary>
    [RelayCommand]
    private async Task ClearTestDataAsync()
    {
        var result = System.Windows.MessageBox.Show(
            "This will delete ALL transactions and sync records.\n\nAre you sure?",
            "Clear Test Data",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            Debug.WriteLine("[DevMode] Clearing test data...");
            
            IsProcessing = true;
            try
            {
                var success = await _dataService.ClearAllDataAsync();
                if (success)
                {
                    // Update daily sales and transaction count locally
                    DailySales = 0;
                    TransactionCount = 0;
                    PendingSyncCount = 0;
                    
                    await RefreshDataAsync();
                    
                    // Send notification (Implementation in next step)
                    try 
                    {
                        var emailService = App.Current.Services.GetService<EmailService>();
                        if (emailService != null)
                        {
                            await emailService.SendTransactionClearNotificationAsync(
                                "Developer Mode / Admin", 
                                0, // Count already cleared 
                                0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[DevMode] Email notification failed: {ex.Message}");
                    }

                    System.Windows.MessageBox.Show(
                        "All test data has been cleared successfully.",
                        "Success",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Failed to clear data. Check logs for details.",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
            finally
            {
                IsProcessing = false;
            }
            
            Debug.WriteLine("[DevMode] Test data clear operation finished");
        }
    }

    /// <summary>
    /// Generates a test PDF invoice.
    /// </summary>
    [RelayCommand]
    private async Task GenerateTestInvoiceAsync()
    {
        Debug.WriteLine("[DevMode] Generating test invoice...");
        
        try
        {
            var pdfService = App.Current.Services.GetService<IPdfInvoiceService>();
            if (pdfService == null)
            {
                Debug.WriteLine("[DevMode] PDF service not available");
                return;
            }
            
            // Create a simple test receipt
            var receipt = new ReceiptModel
            {
                StoreName = "Test Store",
                StoreAddress = "123 Test Street",
                TaxRegistrationNumber = "123456789",
                TransactionNumber = $"TEST-{DateTime.Now:yyyyMMdd-HHmmss}",
                Timestamp = DateTime.Now,
                CashierName = "Developer",
                PaymentMethod = "Test",
                VatRate = 0.14m,
                CurrencySymbol = "EGP",
                Items = new List<ReceiptLineItem>
                {
                    new ReceiptLineItem { ProductName = "Test Product 1", Quantity = 2, UnitPrice = 50.00m },
                    new ReceiptLineItem { ProductName = "Test Product 2", Quantity = 1, UnitPrice = 75.00m }
                }
            };
            
            var pdfPath = await pdfService.GenerateInvoiceAsync(receipt);
            
            if (!string.IsNullOrEmpty(pdfPath))
            {
                // Open the PDF
                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
                Debug.WriteLine($"[DevMode] Test invoice generated: {pdfPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DevMode] Error generating test invoice: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Error generating test invoice:\n{ex.Message}",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        
        await Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private async Task LoadProductsAsync()
    {
        var products = await _dataService.GetAllProductsAsync();
        Products.Clear();
        foreach (var product in products)
            Products.Add(product);
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _dataService.GetCategoriesAsync();
        foreach (var category in categories)
            Categories.Add(category);
    }

    private async Task SearchProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadProductsAsync();
            return;
        }

        var products = await _dataService.SearchProductsAsync(SearchText);
        Products.Clear();
        foreach (var product in products)
            Products.Add(product);
    }

    private async Task FilterProductsAsync()
    {
        if (SelectedCategory == "All")
        {
            await LoadProductsAsync();
            return;
        }

        var products = await _dataService.GetProductsByCategoryAsync(SelectedCategory);
        Products.Clear();
        foreach (var product in products)
            Products.Add(product);
    }

    private async Task LoadRecentTransactionsAsync()
    {
        var transactions = await _dataService.GetTodayTransactionsAsync();
        RecentTransactions.Clear();
        foreach (var transaction in transactions.Take(10))
            RecentTransactions.Add(transaction);

        TransactionCount = RecentTransactions.Count;
    }

    private async Task LoadDailySalesAsync()
    {
        DailySales = await _dataService.GetDailySalesTotalAsync(DateTime.UtcNow);
    }

    private void UpdateTotals()
    {
        SubTotal = CartItems.Sum(c => c.LineTotal);
        // Dynamic tax: calculate per-product using each item's TaxRate
        TaxAmount = CartItems.Sum(c => c.LineTotal * c.TaxRate);
        TotalAmount = SubTotal + TaxAmount;
    }

    partial void OnAmountTenderedChanged(decimal value)
    {
        ChangeAmount = Math.Max(0, value - TotalAmount);
    }

    partial void OnSelectedPaymentMethodChanged(string value)
    {
        // Notify UI to show/hide phone field
        OnPropertyChanged(nameof(ShowPhoneField));
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = SearchProductsAsync();
    }

    private void OnSyncStatusChanged(object? sender, SyncStatusChangedEventArgs e)
    {
        IsOnline = e.IsOnline;
        PendingSyncCount = e.PendingCount;
    }

    private void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        IsUpdateAvailable = true;
        NewVersion = e.NewVersion;
    }

    #endregion
}

/// <summary>
/// Represents an item in the shopping cart.
/// </summary>
public partial class CartItem : ObservableObject
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    
    /// <summary>
    /// Per-product tax rate from database (e.g., 0.14 for 14% VAT)
    /// </summary>
    public decimal TaxRate { get; set; } = 0.14m;

    [ObservableProperty]
    private int _quantity;

    public decimal LineTotal => UnitPrice * Quantity;
    public decimal TaxAmount => LineTotal * TaxRate;

    public new void OnPropertyChanged(string propertyName)
    {
        base.OnPropertyChanged(propertyName);
    }
}
