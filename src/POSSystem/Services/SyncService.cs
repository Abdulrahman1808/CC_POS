using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using POSSystem.Data.Interfaces;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Service for synchronizing local data with the cloud (Supabase).
/// </summary>
public class SyncService : ISyncService
{
    private readonly IDataService _dataService;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly Timer _syncTimer;
    private bool _isOnline;

    public event EventHandler<SyncStatusChangedEventArgs>? SyncStatusChanged;

    public bool IsOnline => _isOnline;
    public int PendingCount { get; private set; }

    public SyncService(IDataService dataService, IConfiguration configuration, HttpClient httpClient)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Check sync status every 30 seconds
        _syncTimer = new Timer(async _ => await CheckAndSyncAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var supabaseUrl = _configuration["Supabase:ConnectionString"];
            
            if (string.IsNullOrEmpty(supabaseUrl))
            {
                _isOnline = false;
                return false;
            }

            // Simple connectivity check
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync("https://www.google.com", cts.Token);
            _isOnline = response.IsSuccessStatusCode;
            
            RaiseSyncStatusChanged();
            return _isOnline;
        }
        catch
        {
            _isOnline = false;
            RaiseSyncStatusChanged();
            return false;
        }
    }

    public async Task<bool> SyncAsync()
    {
        try
        {
            if (!await CheckConnectionAsync())
                return false;

            var pendingRecords = await _dataService.GetPendingSyncRecordsAsync();
            var successCount = 0;

            foreach (var record in pendingRecords)
            {
                try
                {
                    // TODO: Implement actual Supabase sync logic
                    // For now, mark as synced after simulated delay
                    await Task.Delay(100); // Simulate network call
                    
                    await _dataService.MarkAsSyncedAsync(record.Id);
                    successCount++;
                }
                catch (Exception ex)
                {
                    await _dataService.MarkSyncFailedAsync(record.Id, ex.Message);
                }
            }

            await UpdatePendingCountAsync();
            RaiseSyncStatusChanged();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task CheckAndSyncAsync()
    {
        await UpdatePendingCountAsync();
        
        if (PendingCount > 0)
        {
            await SyncAsync();
        }
        else
        {
            await CheckConnectionAsync();
        }
    }

    private async Task UpdatePendingCountAsync()
    {
        try
        {
            PendingCount = await _dataService.GetPendingSyncCountAsync();
        }
        catch
        {
            PendingCount = 0;
        }
    }

    private void RaiseSyncStatusChanged()
    {
        SyncStatusChanged?.Invoke(this, new SyncStatusChangedEventArgs
        {
            IsOnline = _isOnline,
            PendingCount = PendingCount,
            Message = _isOnline ? "Connected" : "Offline"
        });
    }
}
