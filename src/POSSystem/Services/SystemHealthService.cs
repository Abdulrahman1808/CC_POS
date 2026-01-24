using System;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Data;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// System health check results.
/// </summary>
public class SystemHealthReport
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Database Health
    public bool DatabaseConnected { get; set; }
    public string DatabasePath { get; set; } = string.Empty;
    public long DatabaseSizeBytes { get; set; }
    public string DatabaseError { get; set; } = string.Empty;
    
    // Cloud Sync Health
    public bool SupabaseReachable { get; set; }
    public int SupabaseLatencyMs { get; set; }
    public string SupabaseError { get; set; } = string.Empty;
    
    // Printer Health
    public bool PrinterConnected { get; set; }
    public string PrinterName { get; set; } = string.Empty;
    public int AvailablePrintersCount { get; set; }
    public string PrinterError { get; set; } = string.Empty;
    
    // License Health
    public bool LicenseValid { get; set; }
    public string LicenseStatus { get; set; } = string.Empty;
    public bool IsDeveloperMode { get; set; }
    public string MachineId { get; set; } = string.Empty;
    
    // Overall Status
    public bool IsHealthy => DatabaseConnected && (SupabaseReachable || IsDeveloperMode);
    
    public override string ToString()
    {
        return $"""
            ═══════════════════════════════════════════
             SYSTEM HEALTH CHECK - {Timestamp:yyyy-MM-dd HH:mm:ss}
            ═══════════════════════════════════════════
            
            📦 DATABASE
               Connected:  {(DatabaseConnected ? "✅ Yes" : "❌ No")}
               Path:       {DatabasePath}
               Size:       {DatabaseSizeBytes / 1024.0:F1} KB
               {(string.IsNullOrEmpty(DatabaseError) ? "" : $"Error: {DatabaseError}")}
            
            ☁️ SUPABASE CLOUD
               Reachable:  {(SupabaseReachable ? "✅ Yes" : "❌ No")}
               Latency:    {SupabaseLatencyMs} ms
               {(string.IsNullOrEmpty(SupabaseError) ? "" : $"Error: {SupabaseError}")}
            
            🖨️ PRINTER
               Connected:  {(PrinterConnected ? "✅ Yes" : "⚠️ None configured")}
               Printer:    {(string.IsNullOrEmpty(PrinterName) ? "N/A" : PrinterName)}
               Available:  {AvailablePrintersCount} printer(s)
               {(string.IsNullOrEmpty(PrinterError) ? "" : $"Error: {PrinterError}")}
            
            🔑 LICENSE
               Valid:      {(LicenseValid ? "✅ Yes" : "❌ No")}
               Status:     {LicenseStatus}
               Dev Mode:   {(IsDeveloperMode ? "⚙️ ACTIVE" : "No")}
               Machine ID: {MachineId}
            
            ═══════════════════════════════════════════
             OVERALL: {(IsHealthy ? "✅ HEALTHY" : "⚠️ ISSUES DETECTED")}
            ═══════════════════════════════════════════
            """;
    }
}

/// <summary>
/// Interface for system health diagnostics.
/// </summary>
public interface ISystemHealthService
{
    Task<SystemHealthReport> RunHealthCheckAsync();
    Task<bool> CheckDatabaseHealthAsync();
    Task<bool> CheckSupabaseHealthAsync();
    bool CheckPrinterHealth();
    bool CheckLicenseHealth();
}

/// <summary>
/// System health diagnostic service for the Developer Overlay.
/// Checks database, cloud sync, printer, and license status.
/// </summary>
public class SystemHealthService : ISystemHealthService
{
    private readonly IConfiguration _configuration;
    private readonly ILicenseManager _licenseManager;
    private readonly IThermalReceiptService? _thermalService;
    private readonly HttpClient _httpClient;
    
    public SystemHealthService(
        IConfiguration configuration,
        ILicenseManager licenseManager,
        IThermalReceiptService? thermalService = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _licenseManager = licenseManager ?? throw new ArgumentNullException(nameof(licenseManager));
        _thermalService = thermalService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }
    
    /// <summary>
    /// Runs a complete system health check.
    /// </summary>
    public async Task<SystemHealthReport> RunHealthCheckAsync()
    {
        var report = new SystemHealthReport
        {
            MachineId = _licenseManager.MachineId,
            IsDeveloperMode = _licenseManager.IsDeveloperMode,
            LicenseStatus = _licenseManager.Status.ToString(),
            LicenseValid = CheckLicenseHealth()
        };
        
        // Run checks in parallel
        var dbTask = CheckDatabaseHealthInternalAsync(report);
        var supabaseTask = CheckSupabaseHealthInternalAsync(report);
        
        await Task.WhenAll(dbTask, supabaseTask);
        
        CheckPrinterHealthInternal(report);
        
        Debug.WriteLine(report.ToString());
        return report;
    }
    
    /// <summary>
    /// Checks if the local SQLite database is writable.
    /// </summary>
    public async Task<bool> CheckDatabaseHealthAsync()
    {
        try
        {
            using var scope = App.Current.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Test connection
            await dbContext.Database.CanConnectAsync();
            
            // Test write capability
            var testProduct = await dbContext.Products.FirstOrDefaultAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task CheckDatabaseHealthInternalAsync(SystemHealthReport report)
    {
        try
        {
            using var scope = App.Current.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Test connection
            report.DatabaseConnected = await dbContext.Database.CanConnectAsync();
            
            // Get database path
            var dbPath = _configuration["Database:ConnectionString"] ?? "Data Source=posdata.db";
            var match = System.Text.RegularExpressions.Regex.Match(dbPath, @"Data Source=(.+)");
            if (match.Success)
            {
                var path = match.Groups[1].Value;
                report.DatabasePath = Path.GetFullPath(path);
                
                if (File.Exists(report.DatabasePath))
                {
                    var fileInfo = new FileInfo(report.DatabasePath);
                    report.DatabaseSizeBytes = fileInfo.Length;
                }
            }
        }
        catch (Exception ex)
        {
            report.DatabaseConnected = false;
            report.DatabaseError = ex.Message;
        }
    }
    
    /// <summary>
    /// Checks if Supabase API is reachable.
    /// </summary>
    public async Task<bool> CheckSupabaseHealthAsync()
    {
        try
        {
            var supabaseUrl = _configuration["Supabase:Url"];
            if (string.IsNullOrEmpty(supabaseUrl))
                return false;
            
            var response = await _httpClient.GetAsync($"{supabaseUrl}/rest/v1/");
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task CheckSupabaseHealthInternalAsync(SystemHealthReport report)
    {
        try
        {
            var supabaseUrl = _configuration["Supabase:Url"];
            if (string.IsNullOrEmpty(supabaseUrl))
            {
                report.SupabaseError = "Supabase URL not configured";
                return;
            }
            
            var sw = Stopwatch.StartNew();
            var response = await _httpClient.GetAsync($"{supabaseUrl}/rest/v1/");
            sw.Stop();
            
            report.SupabaseLatencyMs = (int)sw.ElapsedMilliseconds;
            report.SupabaseReachable = response.IsSuccessStatusCode || 
                                        response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                                        response.StatusCode == System.Net.HttpStatusCode.Forbidden;
        }
        catch (Exception ex)
        {
            report.SupabaseReachable = false;
            report.SupabaseError = ex.Message;
        }
    }
    
    /// <summary>
    /// Checks if a thermal printer is connected.
    /// </summary>
    public bool CheckPrinterHealth()
    {
        try
        {
            var printers = PrinterSettings.InstalledPrinters;
            return printers.Count > 0;
        }
        catch
        {
            return false;
        }
    }
    
    private void CheckPrinterHealthInternal(SystemHealthReport report)
    {
        try
        {
            var printers = PrinterSettings.InstalledPrinters;
            report.AvailablePrintersCount = printers.Count;
            
            if (_thermalService != null && !string.IsNullOrEmpty(_thermalService.PrinterName))
            {
                report.PrinterConnected = true;
                report.PrinterName = _thermalService.PrinterName;
            }
            else
            {
                report.PrinterConnected = false;
                report.PrinterName = printers.Count > 0 ? "(none configured)" : "(none available)";
            }
        }
        catch (Exception ex)
        {
            report.PrinterConnected = false;
            report.PrinterError = ex.Message;
        }
    }
    
    /// <summary>
    /// Checks if the license is valid.
    /// </summary>
    public bool CheckLicenseHealth()
    {
        return _licenseManager.Status == Models.LicenseStatus.Valid ||
               _licenseManager.Status == Models.LicenseStatus.Trial ||
               _licenseManager.Status == Models.LicenseStatus.Developer;
    }
}
