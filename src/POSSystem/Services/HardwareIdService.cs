using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Service for generating unique hardware-based machine identifiers.
/// Combines CPU ID, Motherboard serial, and MAC address into a unique hash.
/// </summary>
public class HardwareIdService : IHardwareIdService
{
    private string? _cachedMachineId;
    private string? _cachedCpuId;
    private string? _cachedMotherboardSerial;
    private string? _cachedMacAddress;
    
    /// <inheritdoc />
    public string GetMachineId()
    {
        if (!string.IsNullOrEmpty(_cachedMachineId))
            return _cachedMachineId;
        
        try
        {
            var components = new StringBuilder();
            components.Append(GetCpuId());
            components.Append(GetMotherboardSerial());
            components.Append(GetMacAddress());
            
            // Generate SHA256 hash of combined components
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(components.ToString()));
            
            // Convert to uppercase alphanumeric string (32 chars)
            _cachedMachineId = Convert.ToBase64String(hashBytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, 32)
                .ToUpperInvariant();
            
            Debug.WriteLine($"[HardwareIdService] Generated MachineId: {_cachedMachineId}");
            return _cachedMachineId;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HardwareIdService] Error generating ID: {ex.Message}");
            // Fallback to stored or generated GUID
            return GetOrCreateFallbackId();
        }
    }
    
    /// <inheritdoc />
    public string GetCpuId()
    {
        if (!string.IsNullOrEmpty(_cachedCpuId))
            return _cachedCpuId;
        
        _cachedCpuId = GetWmiProperty("Win32_Processor", "ProcessorId") ?? "UNKNOWN_CPU";
        return _cachedCpuId;
    }
    
    /// <inheritdoc />
    public string GetMotherboardSerial()
    {
        if (!string.IsNullOrEmpty(_cachedMotherboardSerial))
            return _cachedMotherboardSerial;
        
        _cachedMotherboardSerial = GetWmiProperty("Win32_BaseBoard", "SerialNumber") ?? "UNKNOWN_MB";
        return _cachedMotherboardSerial;
    }
    
    /// <inheritdoc />
    public string GetMacAddress()
    {
        if (!string.IsNullOrEmpty(_cachedMacAddress))
            return _cachedMacAddress;
        
        _cachedMacAddress = GetWmiProperty(
            "Win32_NetworkAdapterConfiguration", 
            "MACAddress", 
            "IPEnabled = 'True'") ?? "UNKNOWN_MAC";
        return _cachedMacAddress;
    }
    
    /// <summary>
    /// Gets a WMI property value.
    /// </summary>
    private static string? GetWmiProperty(string wmiClass, string property, string? condition = null)
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
                if (!string.IsNullOrEmpty(value) && value != "To Be Filled By O.E.M.")
                {
                    return value.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HardwareIdService] WMI query failed: {ex.Message}");
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets or creates a fallback machine ID stored locally.
    /// Used when WMI queries fail.
    /// </summary>
    private static string GetOrCreateFallbackId()
    {
        var fallbackPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "POSSystem",
            ".machine-id");
        
        try
        {
            if (File.Exists(fallbackPath))
            {
                return File.ReadAllText(fallbackPath).Trim();
            }
            
            // Generate new fallback ID
            var newId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            Directory.CreateDirectory(Path.GetDirectoryName(fallbackPath)!);
            File.WriteAllText(fallbackPath, newId);
            
            Debug.WriteLine($"[HardwareIdService] Created fallback ID: {newId}");
            return newId;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HardwareIdService] Fallback ID error: {ex.Message}");
            return Guid.NewGuid().ToString("N").ToUpperInvariant();
        }
    }
}
