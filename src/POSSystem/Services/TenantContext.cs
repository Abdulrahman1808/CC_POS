using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Manages tenant (business + branch) context for multi-tenancy.
/// Persists context securely using DPAPI encryption.
/// </summary>
public class TenantContext : ITenantContext
{
    private const string TenantFileName = ".tenant";
    private readonly string _tenantFilePath;
    private Guid? _currentBusinessId;
    private Guid? _currentBranchId;
    private string? _currentBranchName;
    
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
    public Guid? CurrentBranchId => _currentBranchId;
    
    /// <inheritdoc />
    public string? CurrentBranchName => _currentBranchName;
    
    /// <inheritdoc />
    public bool IsContextValid => _currentBusinessId.HasValue && _currentBusinessId != Guid.Empty;
    
    /// <inheritdoc />
    public bool IsBranchSelected => _currentBranchId.HasValue && _currentBranchId != Guid.Empty;
    
    /// <inheritdoc />
    public bool IsFullyConfigured => IsContextValid && IsBranchSelected;
    
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
    public void SetBranchContext(Guid branchId, string branchName)
    {
        if (branchId == Guid.Empty)
            throw new ArgumentException("BranchId cannot be empty", nameof(branchId));
        
        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("BranchName cannot be empty", nameof(branchName));
        
        _currentBranchId = branchId;
        _currentBranchName = branchName;
        PersistContext();
        
        Debug.WriteLine($"[TenantContext] Branch context set: {branchName} ({branchId})");
    }
    
    /// <inheritdoc />
    public void ClearContext()
    {
        _currentBusinessId = null;
        _currentBranchId = null;
        _currentBranchName = null;
        
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
        
        Debug.WriteLine("[TenantContext] All context cleared");
    }
    
    /// <inheritdoc />
    public void ClearBranchContext()
    {
        _currentBranchId = null;
        _currentBranchName = null;
        PersistContext();
        
        Debug.WriteLine("[TenantContext] Branch context cleared (business retained)");
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
                _currentBranchId = tenantData.BranchId != Guid.Empty ? tenantData.BranchId : null;
                _currentBranchName = tenantData.BranchName;
                
                Debug.WriteLine($"[TenantContext] Loaded context - Business: {_currentBusinessId}, Branch: {_currentBranchName} ({_currentBranchId})");
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
                BranchId = _currentBranchId ?? Guid.Empty,
                BranchName = _currentBranchName,
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
        public Guid BranchId { get; set; }
        public string? BranchName { get; set; }
        public DateTime PersistedAt { get; set; }
    }
}
