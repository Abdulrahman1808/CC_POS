using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using POSSystem.Data.Interfaces;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Cloud sync service using Supabase REST API.
/// Runs in background via Task.Run, checking SyncQueue every 30 seconds.
/// </summary>
public class CloudSyncService : ISyncService, IDisposable
{
    private readonly IDataService _dataService;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private Timer? _syncTimer;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private readonly string _syncLogPath;
    
    private readonly string? _supabaseUrl;
    private readonly string? _supabaseKey;
    
    private bool _isOnline;
    private int _pendingCount;
    private bool _isDisposed;
    private bool _isSyncing;
    private bool _isStopped;

    public event EventHandler<SyncStatusChangedEventArgs>? SyncStatusChanged;

    public bool IsOnline => _isOnline;
    public int PendingCount => _pendingCount;

    public CloudSyncService(IDataService dataService, IConfiguration configuration)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        _supabaseUrl = _configuration["Supabase:Url"];
        _supabaseKey = _configuration["Supabase:ApiKey"];
        _syncLogPath = Path.Combine(AppContext.BaseDirectory, "sync_log.txt");

        // Configure HTTP client for Supabase REST API
        _httpClient = new HttpClient();
        ConfigureHttpClient();

        // Start background sync timer - runs every 30 seconds via Task.Run
        StartSync();
    }

    private void ConfigureHttpClient()
    {
        if (!string.IsNullOrEmpty(_supabaseUrl) && !string.IsNullOrEmpty(_supabaseKey) && 
            _supabaseKey != "your-anon-key-here")
        {
            _httpClient.BaseAddress = new Uri($"{_supabaseUrl.TrimEnd('/')}/rest/v1/");
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseKey);
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
        }
    }

    private bool IsConfigured => !string.IsNullOrEmpty(_supabaseUrl) && 
                                  !string.IsNullOrEmpty(_supabaseKey) && 
                                  _supabaseKey != "your-anon-key-here";

    /// <summary>
    /// Stops the sync timer. Call before clearing data.
    /// </summary>
    public void StopSync()
    {
        _isStopped = true;
        _syncTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        Debug.WriteLine("[Sync] Timer stopped");
        LogToFile("Sync timer stopped by user request");
    }

    /// <summary>
    /// Starts/restarts the sync timer.
    /// </summary>
    public void StartSync()
    {
        _isStopped = false;
        _syncTimer?.Dispose();
        _syncTimer = new Timer(
            _ => Task.Run(ExecuteSyncCycleAsync),
            null,
            TimeSpan.FromSeconds(5),   // Initial delay
            TimeSpan.FromSeconds(30)   // Interval
        );
        Debug.WriteLine("[Sync] Timer started");
        LogToFile("Sync timer started");
    }

    /// <summary>
    /// Logs sync events to sync_log.txt for debugging.
    /// </summary>
    private void LogToFile(string message)
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
            File.AppendAllText(_syncLogPath, logEntry);
        }
        catch
        {
            // Ignore logging errors
        }
    }

    /// <summary>
    /// Main sync cycle - runs in background via Task.Run
    /// </summary>
    private async Task ExecuteSyncCycleAsync()
    {
        if (_isDisposed || _isSyncing || _isStopped) return;

        // Prevent overlapping sync operations
        if (!await _syncLock.WaitAsync(0)) return;

        _isSyncing = true;
        try
        {
            // Check connectivity
            await CheckConnectionAsync();

            // Update pending count
            _pendingCount = await _dataService.GetPendingSyncCountAsync();

            // Sync if online and have pending items
            if (_isOnline && _pendingCount > 0)
            {
                await SyncAsync();
                LogToFile($"Sync completed. Synced items, {_pendingCount} remaining.");
            }
            else if (!_isOnline && _pendingCount > 0)
            {
                LogToFile($"Offline - {_pendingCount} items pending sync");
            }

            Debug.WriteLine($"[Sync] Cycle complete. Online: {_isOnline}, Pending: {_pendingCount}");
            RaiseSyncStatusChanged("Sync cycle complete");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Sync] ✗ Error: {ex.Message}");
            LogToFile($"ERROR: {ex.Message}");
            RaiseSyncStatusChanged($"Sync error: {ex.Message}");
            // Don't freeze UI - just log and continue
        }
        finally
        {
            _isSyncing = false;
            _syncLock.Release();
        }
    }

    public async Task<bool> CheckConnectionAsync()
    {
        if (!IsConfigured)
        {
            _isOnline = false;
            return false;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync("products?select=id&limit=1", cts.Token);
            _isOnline = response.IsSuccessStatusCode;
            return _isOnline;
        }
        catch
        {
            _isOnline = false;
            return false;
        }
    }

    public async Task<bool> SyncAsync()
    {
        if (!IsConfigured)
        {
            _isOnline = false;
            return false;
        }

        if (!_isOnline)
        {
            await CheckConnectionAsync();
            if (!_isOnline) return false;
        }

        var pendingRecords = await _dataService.GetPendingSyncRecordsAsync();
        var successCount = 0;
        var failCount = 0;

        foreach (var record in pendingRecords)
        {
            try
            {
                var success = await SyncRecordAsync(record);

                if (success)
                {
                    await _dataService.MarkAsSyncedAsync(record.Id);
                    successCount++;
                    Debug.WriteLine($"[Sync] ✓ Successfully uploaded {record.EntityType} #{record.EntityId.ToString()[..8]} to Supabase");
                }
                else
                {
                    await _dataService.MarkSyncFailedAsync(record.Id, "API returned error");
                    failCount++;
                    Debug.WriteLine($"[Sync] ✗ Failed to upload {record.EntityType} #{record.EntityId.ToString()[..8]}");
                }
            }
            catch (HttpRequestException ex)
            {
                await _dataService.MarkSyncFailedAsync(record.Id, ex.Message);
                failCount++;
                _isOnline = false;
                break;
            }
            catch (Exception ex)
            {
                await _dataService.MarkSyncFailedAsync(record.Id, ex.Message);
                failCount++;
            }
        }

        _pendingCount = await _dataService.GetPendingSyncCountAsync();
        RaiseSyncStatusChanged($"Synced {successCount}, failed {failCount}");

        return failCount == 0;
    }

    private async Task<bool> SyncRecordAsync(SyncRecord record)
    {
        var tableName = GetTableName(record.EntityType);

        return record.Operation switch
        {
            SyncOperation.Create => await PostAsync(tableName, record.Payload),
            SyncOperation.Update => await PatchAsync(tableName, record.EntityId, record.Payload),
            SyncOperation.Delete => await DeleteAsync(tableName, record.EntityId),
            _ => false
        };
    }

    private async Task<bool> PostAsync(string table, string? payload)
    {
        if (string.IsNullOrEmpty(payload)) return false;

        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(table, content);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> PatchAsync(string table, Guid entityId, string? payload)
    {
        if (string.IsNullOrEmpty(payload)) return false;

        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _httpClient.PatchAsync($"{table}?id=eq.{entityId}", content);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> DeleteAsync(string table, Guid entityId)
    {
        var response = await _httpClient.DeleteAsync($"{table}?id=eq.{entityId}");
        return response.IsSuccessStatusCode;
    }

    private static string GetTableName(string entityType)
    {
        return entityType.ToLower() switch
        {
            "product" => "products",
            "transaction" => "transactions",
            "transactionitem" => "transaction_items",
            _ => entityType.ToLower() + "s"
        };
    }

    private void RaiseSyncStatusChanged(string? message = null)
    {
        SyncStatusChanged?.Invoke(this, new SyncStatusChangedEventArgs
        {
            IsOnline = _isOnline,
            PendingCount = _pendingCount,
            Message = message
        });
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _syncTimer.Dispose();
        _syncLock.Dispose();
        _httpClient.Dispose();
    }
}
