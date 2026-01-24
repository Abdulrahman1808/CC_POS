using System;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Service for managing license verification and machine identification.
/// Uses hardware identifiers to generate a unique machine ID.
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private LicenseInfo? _cachedLicenseInfo;
    private string? _cachedMachineId;

    public LicenseService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Generates a unique machine identifier from hardware components.
    /// Uses CPU ID, Motherboard Serial, and first MAC address.
    /// </summary>
    public string GetMachineId()
    {
        if (!string.IsNullOrEmpty(_cachedMachineId))
            return _cachedMachineId;

        try
        {
            var components = new StringBuilder();

            // Get CPU ID
            components.Append(GetWmiProperty("Win32_Processor", "ProcessorId"));

            // Get Motherboard Serial
            components.Append(GetWmiProperty("Win32_BaseBoard", "SerialNumber"));

            // Get MAC Address of first network adapter
            components.Append(GetWmiProperty("Win32_NetworkAdapterConfiguration", "MACAddress", "IPEnabled = 'True'"));

            // Generate hash of combined components
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(components.ToString()));
            _cachedMachineId = Convert.ToBase64String(hashBytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, 32)
                .ToUpperInvariant();

            return _cachedMachineId;
        }
        catch
        {
            // Fallback to a GUID-based identifier stored locally
            _cachedMachineId = GetOrCreateFallbackId();
            return _cachedMachineId;
        }
    }

    /// <summary>
    /// Verifies the subscription status with the licensing API.
    /// </summary>
    public async Task<LicenseInfo> VerifySubscriptionAsync()
    {
        try
        {
            var machineId = GetMachineId();
            var apiEndpoint = _configuration["Stripe:ApiEndpoint"];

            if (string.IsNullOrEmpty(apiEndpoint))
            {
                // Development/Mock mode - return trial license
                _cachedLicenseInfo = new LicenseInfo
                {
                    Status = LicenseStatus.Trial,
                    MachineId = machineId,
                    PlanName = "Development Trial",
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                };
                return _cachedLicenseInfo;
            }

            // Call the licensing API
            var request = new
            {
                MachineId = machineId,
                AppVersion = GetAppVersion()
            };

            var response = await _httpClient.PostAsJsonAsync(apiEndpoint, request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LicenseApiResponse>();
                
                _cachedLicenseInfo = new LicenseInfo
                {
                    Status = MapStatus(result?.Status),
                    MachineId = machineId,
                    SubscriptionId = result?.SubscriptionId,
                    PlanName = result?.PlanName,
                    ExpiresAt = result?.ExpiresAt
                };
            }
            else
            {
                _cachedLicenseInfo = new LicenseInfo
                {
                    Status = LicenseStatus.Error,
                    MachineId = machineId,
                    ErrorMessage = $"Server returned {response.StatusCode}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            // Offline mode - check cached license or allow trial
            _cachedLicenseInfo = new LicenseInfo
            {
                Status = LicenseStatus.Trial,
                MachineId = GetMachineId(),
                PlanName = "Offline Mode",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                ErrorMessage = $"Could not connect to licensing server: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _cachedLicenseInfo = new LicenseInfo
            {
                Status = LicenseStatus.Error,
                MachineId = GetMachineId(),
                ErrorMessage = ex.Message
            };
        }

        return _cachedLicenseInfo!;
    }

    public LicenseInfo? GetCachedLicenseInfo() => _cachedLicenseInfo;

    public void ClearCache()
    {
        _cachedLicenseInfo = null;
        _cachedMachineId = null;
    }

    #region Private Methods

    private static string GetWmiProperty(string wmiClass, string property, string? condition = null)
    {
        try
        {
            var query = string.IsNullOrEmpty(condition)
                ? $"SELECT {property} FROM {wmiClass}"
                : $"SELECT {property} FROM {wmiClass} WHERE {condition}";

            using var searcher = new ManagementObjectSearcher(query);
            foreach (var obj in searcher.Get())
            {
                var value = obj[property]?.ToString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }
        catch
        {
            // Ignore WMI errors
        }
        return string.Empty;
    }

    private static string GetOrCreateFallbackId()
    {
        var fallbackPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "POSSystem",
            ".machine-id");

        try
        {
            if (File.Exists(fallbackPath))
                return File.ReadAllText(fallbackPath);

            var newId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            Directory.CreateDirectory(Path.GetDirectoryName(fallbackPath)!);
            File.WriteAllText(fallbackPath, newId);
            return newId;
        }
        catch
        {
            return Guid.NewGuid().ToString("N").ToUpperInvariant();
        }
    }

    private static string GetAppVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString() ?? "1.0.0";
    }

    private static LicenseStatus MapStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "active" or "valid" => LicenseStatus.Valid,
            "expired" => LicenseStatus.Expired,
            "trial" or "trialing" => LicenseStatus.Trial,
            "not_found" or "notfound" => LicenseStatus.NotFound,
            _ => LicenseStatus.Error
        };
    }

    #endregion

    /// <summary>
    /// Internal class for API response deserialization.
    /// </summary>
    private class LicenseApiResponse
    {
        public string? Status { get; set; }
        public string? SubscriptionId { get; set; }
        public string? PlanName { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
