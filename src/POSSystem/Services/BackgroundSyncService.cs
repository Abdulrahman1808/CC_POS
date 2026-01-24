using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using POSSystem.Data.Interfaces;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Background service that syncs local SQLite data to Supabase cloud.
/// Runs every 30 seconds and handles offline/retry scenarios.
/// </summary>
public class BackgroundSyncService : ISyncService, IDisposable
{
    private readonly IDataService _dataService;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly Timer _syncTimer;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    
    private bool _isOnline;
    private int _pendingCount;
    private bool _isDisposed;

    public event EventHandler<SyncStatusChangedEventArgs>? SyncStatusChanged;

    public bool IsOnline => _isOnline;
    public int PendingCount => _pendingCount;

    public BackgroundSyncService(
        IDataService dataService, 
        IConfiguration configuration, 
        HttpClient httpClient)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        ConfigureHttpClient();

        // Start sync timer - runs every 30 seconds
        _syncTimer = new Timer(
            async _ => await ExecuteSyncCycleAsync(),
            null,
            TimeSpan.FromSeconds(5),  // Initial delay
            TimeSpan.FromSeconds(30)  // Interval
        );
    }

    private void ConfigureHttpClient()
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ApiKey"];

        if (!string.IsNullOrEmpty(supabaseUrl))
        {
            _httpClient.BaseAddress = new Uri(supabaseUrl.TrimEnd('/') + "/rest/v1/");
            _httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", supabaseKey);
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
        }
    }

    /// <summary>
    /// Main sync cycle executed by timer.
    /// </summary>
    private async Task ExecuteSyncCycleAsync()
    {
        if (_isDisposed) return;

        // Prevent overlapping sync operations
        if (!await _syncLock.WaitAsync(0)) return;

        try
        {
            // Check connectivity first
            await CheckConnectionAsync();
            
            // Update pending count
            _pendingCount = await _dataService.GetPendingSyncCountAsync();
            
            // If online and have pending items, sync
            if (_isOnline && _pendingCount > 0)
            {
                await SyncAsync();
            }
            
            RaiseSyncStatusChanged("Sync cycle complete");
        }
        catch (Exception ex)
        {
            RaiseSyncStatusChanged($"Sync error: {ex.Message}");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var supabaseUrl = _configuration["Supabase:Url"];
            
            if (string.IsNullOrEmpty(supabaseUrl))
            {
                _isOnline = false;
                return false;
            }

            // Quick connectivity test
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync("", cts.Token);
            _isOnline = response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
            
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
                }
                else
                {
                    await _dataService.MarkSyncFailedAsync(record.Id, "API returned error");
                    failCount++;
                }
            }
            catch (HttpRequestException ex)
            {
                // Network error - mark as failed, will retry
                await _dataService.MarkSyncFailedAsync(record.Id, ex.Message);
                failCount++;
                
                // If network fails, assume offline and stop trying
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

    /// <summary>
    /// Syncs a single record to Supabase.
    /// </summary>
    private async Task<bool> SyncRecordAsync(SyncRecord record)
    {
        var tableName = GetTableName(record.EntityType);
        
        return record.Operation switch
        {
            SyncOperation.Create => await PostToSupabaseAsync(tableName, record.Payload),
            SyncOperation.Update => await PatchSupabaseAsync(tableName, record.EntityId, record.Payload),
            SyncOperation.Delete => await DeleteFromSupabaseAsync(tableName, record.EntityId),
            _ => false
        };
    }

    private async Task<bool> PostToSupabaseAsync(string table, string? payload)
    {
        if (string.IsNullOrEmpty(payload)) return false;

        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(table, content);
        
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> PatchSupabaseAsync(string table, Guid entityId, string? payload)
    {
        if (string.IsNullOrEmpty(payload)) return false;

        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PatchAsync($"{table}?id=eq.{entityId}", content);
        
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> DeleteFromSupabaseAsync(string table, Guid entityId)
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
    }
}
