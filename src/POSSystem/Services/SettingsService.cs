using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Services;

/// <summary>
/// Service for storing and retrieving local app settings.
/// Manages BusinessId pairing for cloud sync.
/// </summary>
public interface ISettingsService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<Guid?> GetBusinessIdAsync();
    Task SetBusinessIdAsync(Guid businessId);
    Task<BusinessProfile?> GetBusinessProfileAsync();
    Task SaveBusinessProfileAsync(BusinessProfile profile);
}

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _context;
    private BusinessProfile? _cachedProfile;

    public SettingsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetAsync(string key)
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task SetAsync(string key, string value)
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            setting = new AppSettings { Key = key, Value = value };
            await _context.Settings.AddAsync(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        Debug.WriteLine($"[Settings] Saved: {key} = {value[..Math.Min(20, value.Length)]}...");
    }

    public async Task<Guid?> GetBusinessIdAsync()
    {
        var value = await GetAsync("business_id");
        if (Guid.TryParse(value, out var id))
            return id;
        return null;
    }

    public async Task SetBusinessIdAsync(Guid businessId)
    {
        await SetAsync("business_id", businessId.ToString());
        Debug.WriteLine($"[Settings] BusinessId paired: {businessId}");
    }

    public async Task<BusinessProfile?> GetBusinessProfileAsync()
    {
        if (_cachedProfile != null) return _cachedProfile;

        _cachedProfile = await _context.BusinessProfiles.FirstOrDefaultAsync();
        return _cachedProfile;
    }

    public async Task SaveBusinessProfileAsync(BusinessProfile profile)
    {
        var existing = await _context.BusinessProfiles
            .FirstOrDefaultAsync(p => p.Id == profile.Id);

        if (existing == null)
        {
            await _context.BusinessProfiles.AddAsync(profile);
        }
        else
        {
            existing.OwnerId = profile.OwnerId;
            existing.BusinessName = profile.BusinessName;
            existing.PlanName = profile.PlanName;
            existing.MaxEmployees = profile.MaxEmployees;
            existing.CloudSyncEnabled = profile.CloudSyncEnabled;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _cachedProfile = profile;
        Debug.WriteLine($"[Settings] Business profile saved: {profile.BusinessName}");
    }
}
