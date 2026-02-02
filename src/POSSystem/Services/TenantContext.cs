using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Manages tenant (business) context for multi-tenancy.
/// Persists the BusinessId securely alongside the license key.
/// </summary>
public class TenantContext : ITenantContext
{
    private const string TenantFileName = ".tenant";
    private readonly string _tenantFilePath;
    private Guid? _currentBusinessId;
    
    public TenantContext()
    {
        _tenantFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "POSSystem",
            TenantFileName);
        
        // Try to load persisted context on startup
        LoadPersistedContext();
    }
    
    /// <inheritdoc />
    public Guid? CurrentBusinessId => _currentBusinessId;
    
    /// <inheritdoc />
    public bool IsContextValid => _currentBusinessId.HasValue && _currentBusinessId != Guid.Empty;
    
    /// <inheritdoc />
    public void SetBusinessContext(Guid businessId)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("BusinessId cannot be empty", nameof(businessId));
        
        _currentBusinessId = businessId;
        PersistContext();
        
        Debug.WriteLine($"[TenantContext] Business context set: {businessId}");
    }
    
    /// <inheritdoc />
    public void ClearContext()
    {
        _currentBusinessId = null;
        
        try
        {
            if (File.Exists(_tenantFilePath))
            {
                File.Delete(_tenantFilePath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TenantContext] Failed to delete tenant file: {ex.Message}");
        }
        
        Debug.WriteLine("[TenantContext] Business context cleared");
    }
    
    /// <inheritdoc />
    public bool LoadPersistedContext()
    {
        try
        {
            if (!File.Exists(_tenantFilePath))
            {
                Debug.WriteLine("[TenantContext] No persisted context found");
                return false;
            }
            
            var encryptedData = File.ReadAllBytes(_tenantFilePath);
            var decryptedJson = DecryptData(encryptedData);
            
            var tenantData = JsonSerializer.Deserialize<TenantData>(decryptedJson);
            
            if (tenantData != null && tenantData.BusinessId != Guid.Empty)
            {
                _currentBusinessId = tenantData.BusinessId;
                Debug.WriteLine($"[TenantContext] Loaded persisted context: {_currentBusinessId}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TenantContext] Failed to load persisted context: {ex.Message}");
        }
        
        return false;
    }
    
    /// <summary>
    /// Persists the current context securely using DPAPI.
    /// </summary>
    private void PersistContext()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_tenantFilePath)!);
            
            var tenantData = new TenantData
            {
                BusinessId = _currentBusinessId ?? Guid.Empty,
                PersistedAt = DateTime.UtcNow
            };
            
            var json = JsonSerializer.Serialize(tenantData);
            var encryptedData = EncryptData(json);
            
            File.WriteAllBytes(_tenantFilePath, encryptedData);
            Debug.WriteLine($"[TenantContext] Context persisted to {_tenantFilePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TenantContext] Failed to persist context: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Encrypts data using Windows DPAPI (user scope).
    /// </summary>
    private static byte[] EncryptData(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        return ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
    }
    
    /// <summary>
    /// Decrypts data using Windows DPAPI (user scope).
    /// </summary>
    private static string DecryptData(byte[] encryptedData)
    {
        var decryptedBytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
    
    /// <summary>
    /// Internal data structure for persistence.
    /// </summary>
    private class TenantData
    {
        public Guid BusinessId { get; set; }
        public DateTime PersistedAt { get; set; }
    }
}
