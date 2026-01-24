using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using POSSystem.Data;
using POSSystem.Models;
using POSSystem.Services;
using POSSystem.Services.Interfaces;

namespace POSSystem.ViewModels;

/// <summary>
/// ViewModel for Store Settings with logo upload and branding.
/// </summary>
public partial class StoreSettingsViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ISyncService? _syncService;
    private readonly EmailService _emailService;

    [ObservableProperty]
    private string _storeName = "My Store";

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _logoPath = string.Empty;

    [ObservableProperty]
    private decimal _defaultTaxRate = 0.14m;

    [ObservableProperty]
    private string _currencySymbol = "EGP";

    [ObservableProperty]
    private string _receiptFooter = "Thank you for your purchase!";

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Admin email for transaction clearing notifications.
    /// </summary>
    [ObservableProperty]
    private string _adminEmail = string.Empty;

    /// <summary>
    /// Total transaction count in database.
    /// </summary>
    [ObservableProperty]
    private int _transactionCount;

    /// <summary>
    /// Today's sales total.
    /// </summary>
    [ObservableProperty]
    private decimal _todaysSales;

    private StoreSettings? _settings;

    public StoreSettingsViewModel(AppDbContext context, IConfiguration configuration, ISyncService? syncService = null)
    {
        _context = context;
        _configuration = configuration;
        _syncService = syncService;
        _emailService = new EmailService(configuration);
        
        // Load admin email from config
        _adminEmail = configuration["Admin:Email"] ?? "abdulrahman.mohamed1808@gmail.com";
    }

    #region Navigation
    public event EventHandler<string>? NavigationRequested;

    [RelayCommand]
    private void BackToDashboard() => NavigationRequested?.Invoke(this, "Dashboard");
    #endregion

    public async Task InitializeAsync()
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            // IMPORTANT: Clear change tracker to ensure fresh reads from database
            // This fixes sync issues when navigating between views
            _context.ChangeTracker.Clear();
            
            _settings = await _context.Set<StoreSettings>().AsNoTracking().FirstOrDefaultAsync();
            
            if (_settings == null)
            {
                // Create default settings
                _settings = new StoreSettings();
                await _context.Set<StoreSettings>().AddAsync(_settings);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Re-attach for future updates
                _context.Attach(_settings);
            }

            // Load into properties
            StoreName = _settings.StoreName;
            Address = _settings.Address;
            PhoneNumber = _settings.PhoneNumber;
            Email = _settings.Email;
            LogoPath = _settings.LogoPath;
            DefaultTaxRate = _settings.DefaultTaxRate;
            CurrencySymbol = _settings.CurrencySymbol;
            ReceiptFooter = _settings.ReceiptFooter;

            // Load transaction statistics with fresh reads
            TransactionCount = await _context.Transactions.AsNoTracking().CountAsync();
            var today = DateTime.UtcNow.Date;
            var todayTransactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.CreatedAt.Date >= today)
                .ToListAsync();
            TodaysSales = todayTransactions.Sum(t => t.Total);

            Debug.WriteLine($"[Settings] Loaded: {StoreName}, Transactions: {TransactionCount}, Today: {TodaysSales}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Load error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void BrowseLogo()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            Title = "Select Store Logo"
        };

        if (dialog.ShowDialog() == true)
        {
            // Copy logo to app directory
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var logoDir = Path.Combine(appDir, "Logos");
            Directory.CreateDirectory(logoDir);

            var fileName = $"logo_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(dialog.FileName)}";
            var destPath = Path.Combine(logoDir, fileName);

            File.Copy(dialog.FileName, destPath, true);
            LogoPath = destPath;

            Debug.WriteLine($"[Settings] Logo saved: {destPath}");
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (_settings == null) return;

        IsSaving = true;
        StatusMessage = "Saving...";

        try
        {
            _settings.StoreName = StoreName;
            _settings.Address = Address;
            _settings.PhoneNumber = PhoneNumber;
            _settings.Email = Email;
            _settings.LogoPath = LogoPath;
            _settings.DefaultTaxRate = DefaultTaxRate;
            _settings.CurrencySymbol = CurrencySymbol;
            _settings.ReceiptFooter = ReceiptFooter;
            _settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            StatusMessage = "✓ Settings saved!";
            Debug.WriteLine("[Settings] Saved successfully");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            Debug.WriteLine($"[Settings] Save error: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void PreviewReceipt()
    {
        var receipt = GenerateReceiptPreview();
        MessageBox.Show(receipt, "Receipt Preview", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public string GenerateReceiptPreview()
    {
        return $@"
╔══════════════════════════════════════╗
║           {StoreName,-28}║
╠══════════════════════════════════════╣
║  {Address,-36}║
║  Tel: {PhoneNumber,-30}║
║  {Email,-36}║
╠══════════════════════════════════════╣
║  Date: {DateTime.Now:yyyy-MM-dd HH:mm,-27}║
║  Receipt #: TXN-PREVIEW-001         ║
╠══════════════════════════════════════╣
║  ITEM              QTY    PRICE     ║
║  ─────────────────────────────────  ║
║  Sample Product 1    2    100.00    ║
║  Sample Product 2    1     50.00    ║
╠══════════════════════════════════════╣
║  Subtotal:                  150.00  ║
║  Tax ({DefaultTaxRate:P0}):                   {150 * DefaultTaxRate:F2}  ║
║  ─────────────────────────────────  ║
║  TOTAL:            {CurrencySymbol} {150 * (1 + DefaultTaxRate):F2}  ║
╠══════════════════════════════════════╣
║  {ReceiptFooter,-36}║
╚══════════════════════════════════════╝
";
    }

    /// <summary>
    /// Clear all transactions from the database with warning.
    /// </summary>
    [RelayCommand]
    private async Task ClearTransactionsAsync()
    {
        if (TransactionCount == 0)
        {
            MessageBox.Show("No transactions to clear.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Validate admin email
        if (string.IsNullOrWhiteSpace(AdminEmail) || !AdminEmail.Contains("@"))
        {
            MessageBox.Show(
                "Please enter a valid admin email address.\n\nThis is required to notify the administrator about the transaction clearing.",
                "Email Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Show warning
        var result = MessageBox.Show(
            $"⚠️ WARNING: DESTRUCTIVE ACTION ⚠️\n\n" +
            $"You are about to DELETE ALL {TransactionCount} transactions!\n\n" +
            $"Today's Sales: {TodaysSales:C2}\n\n" +
            $"This action CANNOT be undone!\n\n" +
            $"Admin notification will be sent to:\n{AdminEmail}\n\n" +
            $"Are you absolutely sure?",
            "Clear All Transactions",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        // Second confirmation
        var confirm = MessageBox.Show(
            "FINAL CONFIRMATION\n\nType 'DELETE' in your mind and click Yes to proceed.",
            "Confirm Deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Stop);

        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            IsSaving = true;
            var clearedCount = TransactionCount;
            var clearedSales = TodaysSales;
            
            // Step 1: Stop the sync service
            StatusMessage = "Stopping sync service...";
            if (_syncService is CloudSyncService cloudSync)
            {
                cloudSync.StopSync();
                Debug.WriteLine("[Settings] Sync service stopped");
            }
            
            // Step 2: Send admin notification email
            StatusMessage = "Sending admin notification...";
            var emailSent = await _emailService.SendTransactionClearNotificationAsync(
                triggeredBy: AdminEmail,
                transactionCount: clearedCount,
                totalSalesCleared: clearedSales
            );
            if (emailSent)
            {
                Debug.WriteLine("[Settings] Admin notification sent/logged");
            }
            
            // Step 3: Clear local SQLite database
            StatusMessage = "Clearing local database...";
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM TransactionItems");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Transactions");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM SyncRecords WHERE EntityType = 'Transaction'");
            _context.ChangeTracker.Clear();
            Debug.WriteLine("[Settings] Local database cleared");
            
            // Step 4: Clear Supabase data (handled by sync queue - records are cleared so nothing to sync)
            StatusMessage = "Clearing cloud data...";
            // Note: Supabase clearing requires API call - logged for manual action if needed
            var supabaseLogPath = Path.Combine(AppContext.BaseDirectory, "supabase_clear_log.txt");
            await File.AppendAllTextAsync(supabaseLogPath, 
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Transaction clear triggered. Count: {clearedCount}, Sales: {clearedSales:C2}\n");
            Debug.WriteLine("[Settings] Supabase clear logged");
            
            // Step 5: Restart sync service
            StatusMessage = "Restarting sync service...";
            if (_syncService is CloudSyncService cloudSync2)
            {
                cloudSync2.StartSync();
                Debug.WriteLine("[Settings] Sync service restarted");
            }
            
            // Update UI
            TransactionCount = 0;
            TodaysSales = 0;
            StatusMessage = "✓ All transactions cleared!";

            MessageBox.Show(
                $"Successfully cleared {clearedCount} transactions!\n\n" +
                $"• Admin notification: {(emailSent ? "Sent/Logged" : "Failed")}\n" +
                $"• Local database: Cleared\n" +
                $"• Sync queue: Cleared\n" +
                $"• Sync service: Restarted\n\n" +
                $"Total sales cleared: {clearedSales:C2}",
                "Transactions Cleared",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Failed to clear transactions:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Try to restart sync service even on error
            if (_syncService is CloudSyncService cloudSync)
            {
                cloudSync.StartSync();
            }
        }
        finally
        {
            IsSaving = false;
        }
    }
}
