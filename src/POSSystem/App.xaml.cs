using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Data;
using POSSystem.Data.Interfaces;
using POSSystem.Services;
using POSSystem.Services.Interfaces;
using POSSystem.ViewModels;
using Velopack;

namespace POSSystem;

/// <summary>
/// Application entry point with DI configuration.
/// </summary>
public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;
    private IConfiguration _configuration = null!;
    private MainWindow? _mainWindow;
    
    /// <summary>
    /// Gets the current application's service provider for DI resolution.
    /// </summary>
    public static new App Current => (App)Application.Current;
    
    /// <summary>
    /// Gets the DI service provider.
    /// </summary>
    public IServiceProvider Services => _serviceProvider;

    [STAThread]
    public static void Main(string[] args)
    {
        Debug.WriteLine("[App] === POS System Starting ===");
        
        // Velopack initialization
        try
        {
            Debug.WriteLine("[App] Initializing Velopack...");
            VelopackApp.Build().Run();
            Debug.WriteLine("[App] Velopack initialized.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Velopack error (ignored): {ex.Message}");
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        Debug.WriteLine("[App] Application_Startup called");
        
        try
        {
            // Build configuration
            Debug.WriteLine("[App] Loading configuration...");
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            Debug.WriteLine("[App] Configuration loaded.");

            // Setup dependency injection
            Debug.WriteLine("[App] Configuring services...");
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            Debug.WriteLine("[App] Services configured.");
            
            // Validate persisted branch context on startup
            ValidateBranchContextOnStartup();

            // Show main window FIRST (responsive UI)
            Debug.WriteLine("[App] Showing main window...");
            ShowMainWindow();
            Debug.WriteLine("[App] Main window shown.");
            
            // Initialize database in background (non-blocking)
            Debug.WriteLine("[App] Starting async database initialization...");
            _ = InitializeDatabaseInBackgroundAsync();

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] FATAL ERROR in startup: {ex}");
            MessageBox.Show($"Failed to start application:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <summary>
    /// Initializes database in background without blocking UI thread.
    /// </summary>
    private async Task InitializeDatabaseInBackgroundAsync()
    {
        try
        {
            await Task.Run(async () =>
            {
                await InitializeDatabaseAsync();
            });
            Debug.WriteLine("[App] Database initialized (background).");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Database init error: {ex.Message}");
            await Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show($"Database initialization error:\n{ex.Message}", 
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }
    }
    
    /// <summary>
    /// Validates that persisted branch context was loaded correctly on startup.
    /// This ensures hardware-locked machines automatically load their branch binding.
    /// </summary>
    private void ValidateBranchContextOnStartup()
    {
        try
        {
            var tenantContext = _serviceProvider.GetService<ITenantContext>();
            if (tenantContext == null)
            {
                Debug.WriteLine("[App] ⚠️ TenantContext not available");
                return;
            }
            
            // Log current tenant state
            Debug.WriteLine($"[App] Tenant Context Status:");
            Debug.WriteLine($"  - IsContextValid: {tenantContext.IsContextValid}");
            Debug.WriteLine($"  - BusinessId: {tenantContext.CurrentBusinessId}");
            Debug.WriteLine($"  - IsBranchSelected: {tenantContext.IsBranchSelected}");
            Debug.WriteLine($"  - BranchId: {tenantContext.CurrentBranchId}");
            Debug.WriteLine($"  - BranchName: {tenantContext.CurrentBranchName}");
            Debug.WriteLine($"  - IsFullyConfigured: {tenantContext.IsFullyConfigured}");
            
            if (tenantContext.IsFullyConfigured)
            {
                Debug.WriteLine($"[App] ✅ Hardware locked to branch: {tenantContext.CurrentBranchName}");
            }
            else if (tenantContext.IsContextValid)
            {
                Debug.WriteLine("[App] ⚠️ Business context set but no branch selected (will prompt on license check)");
            }
            else
            {
                Debug.WriteLine("[App] ℹ️ No tenant context - fresh installation or cleared license");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Error validating branch context: {ex.Message}");
        }
    }

    /// <summary>
    /// Global exception handler - shows error instead of crashing silently.
    /// </summary>
    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Debug.WriteLine($"[App] UNHANDLED EXCEPTION: {e.Exception}");
        
        MessageBox.Show(
            $"An error occurred:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
            "Application Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );

        e.Handled = true; // Prevent crash, allow user to see the error
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        services.AddSingleton(_configuration);

        // HttpClient
        services.AddSingleton<HttpClient>();

        // Database - Use AppData folder to avoid permission issues in Program Files
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "POSSystem");
        Directory.CreateDirectory(appDataPath); // Ensure directory exists
        
        var dbPath = Path.Combine(appDataPath, "posdata.db");
        var connectionString = $"Data Source={dbPath}";
        
        Debug.WriteLine($"[App] Database path: {dbPath}");
        
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));

        // Data Services - Singleton to avoid disposed context issues with background sync
        services.AddSingleton<IDataService, SqliteDataService>();

        // Business Services
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<IUpdateService, UpdateService>();

        // Cloud Sync Service
        services.AddSingleton<ISyncService, CloudSyncService>();

        // Settings & Auth Services (Cloud Bridge)
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddSingleton<IAuthService>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var settingsService = sp.CreateScope().ServiceProvider.GetRequiredService<ISettingsService>();
            var licenseService = sp.GetRequiredService<ILicenseService>();
            return new DesktopAuthService(httpClient, _configuration, settingsService, licenseService);
        });
        
        // Realtime Service for live updates from web dashboard
        services.AddSingleton<RealtimeService>(sp => new RealtimeService(_configuration));

        // ===== HARDWARE & LICENSE SERVICES =====
        services.AddSingleton<IHardwareIdService, HardwareIdService>();
        
        // TenantContext must be registered before LicenseManager (dependency)
        services.AddSingleton<ITenantContext, TenantContext>();
        services.AddSingleton<ILicenseManager, LicenseManager>();

        // ===== DOCUMENT GENERATION SERVICES =====
        // Thermal Receipt Service (ESCPOS_NET - prints to debug in dev mode)
        services.AddSingleton<IThermalReceiptService, ThermalReceiptService>();
        
        // PDF Invoice Service (QuestPDF - adds watermark in dev mode)
        services.AddSingleton<IPdfInvoiceService, PdfInvoiceService>();
        
        // Document Service Facade (coordinates thermal + PDF + ETA QR)
        services.AddSingleton<IDocumentService, DocumentService>();
        
        // System Health Service (diagnostics for dev overlay)
        services.AddSingleton<ISystemHealthService, SystemHealthService>();
        
        // Email Service (admin notifications)
        services.AddSingleton<IEmailService, EmailService>();
        
        // Payment Service (Stripe for card payments)
        services.AddSingleton<IPaymentService, StripePaymentService>();


        // ViewModels
        services.AddTransient<LicenseViewModel>(sp =>
            new LicenseViewModel(
                sp.GetRequiredService<ILicenseService>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILicenseManager>()
            ));
        services.AddTransient<LoginViewModel>(sp =>
            new LoginViewModel(
                sp.GetRequiredService<ILicenseService>(),
                _configuration["Stripe:CheckoutUrl"]
            ));
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<StoreSettingsViewModel>(sp => 
            new StoreSettingsViewModel(
                sp.GetRequiredService<AppDbContext>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetService<ISyncService>()
            ));
        services.AddTransient<MainViewModel>();
    }

    private async System.Threading.Tasks.Task InitializeDatabaseAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dataService.InitializeDatabaseAsync();

#if DEBUG
        // Seed sample products in debug mode (only if DB is COMPLETELY empty)
        Debug.WriteLine("[App] Checking if database needs seeding...");
        await DataSeeder.SeedIfEmptyAsync(dataService, dbContext);
#endif
    }

    private void ShowMainWindow()
    {
        Debug.WriteLine("[App] Step 1: Creating MainViewModel...");
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        
        Debug.WriteLine("[App] Step 2: Creating LicenseViewModel...");
        var licenseViewModel = _serviceProvider.GetRequiredService<LicenseViewModel>();

        Debug.WriteLine("[App] Step 3: Setting initial view to LicenseViewModel...");
        mainViewModel.SetCurrentView(licenseViewModel);

        // Handle successful license verification
        licenseViewModel.LicenseVerified += async (s, e) =>
        {
            Debug.WriteLine("[App] LicenseVerified event fired! Transitioning to dashboard...");
            await ShowDashboardAsync(mainViewModel);
        };

        // Handle logout from dashboard
        mainViewModel.LogoutRequested += (s, e) =>
        {
            Debug.WriteLine("[App] LogoutRequested event fired...");
            var newLicenseVm = _serviceProvider.GetRequiredService<LicenseViewModel>();
            newLicenseVm.LicenseVerified += async (s2, e2) => await ShowDashboardAsync(mainViewModel);
            mainViewModel.SetCurrentView(newLicenseVm);
            _ = newLicenseVm.CheckLicenseCommand.ExecuteAsync(null);
        };

        Debug.WriteLine("[App] Step 4: Creating MainWindow...");
        _mainWindow = new MainWindow();
        
        Debug.WriteLine("[App] Step 5: Setting MainWindow.DataContext...");
        _mainWindow.SetViewModel(mainViewModel);
        
        Debug.WriteLine("[App] Step 6: Setting Application.Current.MainWindow...");
        Application.Current.MainWindow = _mainWindow;
        
        Debug.WriteLine("[App] Step 7: Showing MainWindow...");
        _mainWindow.Show();

        Debug.WriteLine("[App] Step 8: Starting license verification...");
        _ = licenseViewModel.CheckLicenseCommand.ExecuteAsync(null);
        
        Debug.WriteLine("[App] MainWindow initialization complete.");
    }

    private async System.Threading.Tasks.Task ShowDashboardAsync(MainViewModel mainViewModel)
    {
        Debug.WriteLine("[App] ShowDashboardAsync called...");
        
        try
        {
            Debug.WriteLine("[App] Creating DashboardViewModel...");
            var dashboardViewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
            
            // Wire up navigation to other views
            dashboardViewModel.NavigationRequested += (s, viewName) =>
            {
                Debug.WriteLine($"[App] Navigation requested: {viewName}");
                NavigateToView(mainViewModel, viewName, dashboardViewModel);
            };
            
            Debug.WriteLine("[App] Setting dashboard as current view...");
            mainViewModel.SetCurrentView(dashboardViewModel);
            
            Debug.WriteLine("[App] Initializing dashboard data...");
            await dashboardViewModel.InitializeAsync();
            
            Debug.WriteLine("[App] Dashboard transition complete!");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] ERROR in ShowDashboardAsync: {ex}");
            MessageBox.Show($"Failed to load dashboard:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NavigateToView(MainViewModel mainViewModel, string viewName, DashboardViewModel dashboardViewModel)
    {
        try
        {
            ObservableObject? viewModel = null;
            
            if (viewName == "Inventory")
            {
                var inv = _serviceProvider.GetRequiredService<InventoryViewModel>();
                inv.NavigationRequested += (s, v) => NavigateToView(mainViewModel, v, dashboardViewModel);
                _ = inv.InitializeAsync();
                viewModel = inv;
            }
            else if (viewName == "Reports")
            {
                var rep = _serviceProvider.GetRequiredService<ReportsViewModel>();
                rep.NavigationRequested += (s, v) => NavigateToView(mainViewModel, v, dashboardViewModel);
                _ = rep.InitializeAsync();
                viewModel = rep;
            }
            else if (viewName == "Settings")
            {
                var set = _serviceProvider.GetRequiredService<StoreSettingsViewModel>();
                set.NavigationRequested += (s, v) => NavigateToView(mainViewModel, v, dashboardViewModel);
                _ = set.InitializeAsync();
                viewModel = set;
            }
            else if (viewName == "Dashboard")
            {
                viewModel = dashboardViewModel;
            }

            if (viewModel != null)
            {
                mainViewModel.SetCurrentView(viewModel!);
                Debug.WriteLine($"[App] Navigated to: {viewName}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Navigation error: {ex.Message}");
            MessageBox.Show($"Navigation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Debug.WriteLine("[App] OnExit called, disposing services...");
        
        // Dispose sync service
        if (_serviceProvider?.GetService<ISyncService>() is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        Debug.WriteLine("[App] Shutdown complete.");
        base.OnExit(e);
    }
}
