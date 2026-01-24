using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using POSSystem.Services.Interfaces;
using Velopack;
using Velopack.Sources;

namespace POSSystem.Services;

/// <summary>
/// Service for managing application auto-updates via Velopack.
/// </summary>
public class UpdateService : IUpdateService
{
    private readonly IConfiguration _configuration;
    private readonly UpdateManager? _updateManager;
    private UpdateInfo? _updateInfo;

    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

    public bool IsUpdateAvailable => _updateInfo != null;
    public string? NewVersion => _updateInfo?.TargetFullRelease?.Version?.ToString();

    public UpdateService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        var repoUrl = _configuration["Updates:GitHubRepoUrl"];
        
        if (!string.IsNullOrEmpty(repoUrl))
        {
            try
            {
                var source = new GithubSource(repoUrl, null, false);
                _updateManager = new UpdateManager(source);
            }
            catch
            {
                // Velopack not initialized - development mode
                _updateManager = null;
            }
        }
        else
        {
            _updateManager = null;
        }
    }

    public string GetCurrentVersion()
    {
        try
        {
            // Use assembly version as fallback
            return Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString() ?? "1.0.0-dev";
        }
        catch
        {
            return "1.0.0-dev";
        }
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            if (_updateManager == null)
            {
                return false;
            }

            _updateInfo = await _updateManager.CheckForUpdatesAsync();

            if (_updateInfo != null)
            {
                UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs
                {
                    NewVersion = _updateInfo.TargetFullRelease?.Version?.ToString() ?? "Unknown",
                    ReleaseNotes = null,
                    DownloadSize = _updateInfo.TargetFullRelease?.Size
                });
                return true;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task ApplyUpdateAsync()
    {
        try
        {
            if (_updateManager == null || _updateInfo == null)
            {
                return;
            }

            await _updateManager.DownloadUpdatesAsync(_updateInfo);
            _updateManager.ApplyUpdatesAndRestart(_updateInfo);
        }
        catch (Exception)
        {
            throw;
        }
    }
}
