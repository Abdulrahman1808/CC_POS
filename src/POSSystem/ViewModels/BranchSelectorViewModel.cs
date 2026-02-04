using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.ViewModels;

/// <summary>
/// ViewModel for Branch Selection dialog.
/// Fetches branches from Supabase and handles selection + hardware locking.
/// </summary>
public partial class BranchSelectorViewModel : ObservableObject
{
    private readonly ITenantContext _tenantContext;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    public event Action<bool>? RequestClose;
    
    [ObservableProperty]
    private ObservableCollection<Branch> _branches = new();
    
    [ObservableProperty]
    private Branch? _selectedBranch;
    
    [ObservableProperty]
    private string _businessName = "Loading...";
    
    [ObservableProperty]
    private bool _isLoading = true;
    
    [ObservableProperty]
    private bool _isOffline;
    
    [ObservableProperty]
    private bool _hasBranches;
    
    public bool CanConfirm => SelectedBranch != null && !IsLoading && !IsOffline;
    
    public BranchSelectorViewModel(
        ITenantContext tenantContext,
        IConfiguration configuration)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        _httpClient = new HttpClient();
        ConfigureHttpClient();
        
        // Load branches on construction
        _ = LoadBranchesAsync();
    }
    
    private void ConfigureHttpClient()
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ApiKey"];
        
        if (!string.IsNullOrEmpty(supabaseUrl))
        {
            _httpClient.BaseAddress = new Uri(supabaseUrl.TrimEnd('/') + "/rest/v1/");
            _httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
            
            // Add business ID header for RLS
            if (_tenantContext.CurrentBusinessId.HasValue)
            {
                _httpClient.DefaultRequestHeaders.Add("X-Business-Id", _tenantContext.CurrentBusinessId.Value.ToString());
            }
        }
    }
    
    private async Task LoadBranchesAsync()
    {
        IsLoading = true;
        IsOffline = false;
        HasBranches = false;
        
        try
        {
            // Verify we have a business context
            if (!_tenantContext.IsContextValid)
            {
                Debug.WriteLine("[BranchSelector] No valid business context");
                IsOffline = true;
                IsLoading = false;
                return;
            }
            
            var businessId = _tenantContext.CurrentBusinessId!.Value;
            
            // Fetch business name first
            await LoadBusinessNameAsync(businessId);
            
            // Fetch branches for this business
            var response = await _httpClient.GetAsync(
                $"branches?business_id=eq.{businessId}&is_active=eq.true&select=*");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var branches = JsonSerializer.Deserialize<Branch[]>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
                
                if (branches != null && branches.Length > 0)
                {
                    Branches = new ObservableCollection<Branch>(branches);
                    HasBranches = true;
                    
                    // Auto-select first branch if only one
                    if (branches.Length == 1)
                    {
                        SelectedBranch = branches[0];
                    }
                    
                    Debug.WriteLine($"[BranchSelector] Loaded {branches.Length} branches");
                }
                else
                {
                    Debug.WriteLine("[BranchSelector] No branches found for this business");
                    // Create default branch suggestion
                    BusinessName += " (No branches configured)";
                }
            }
            else
            {
                Debug.WriteLine($"[BranchSelector] Failed to load branches: {response.StatusCode}");
                IsOffline = true;
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"[BranchSelector] Network error: {ex.Message}");
            IsOffline = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BranchSelector] Error: {ex.Message}");
            IsOffline = true;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanConfirm));
        }
    }
    
    private async Task LoadBusinessNameAsync(Guid businessId)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"businesses?id=eq.{businessId}&select=name");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.GetArrayLength() > 0)
                {
                    BusinessName = doc.RootElement[0].GetProperty("name").GetString() ?? "Unknown Business";
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BranchSelector] Failed to load business name: {ex.Message}");
            BusinessName = "Unknown Business";
        }
    }
    
    partial void OnSelectedBranchChanged(Branch? value)
    {
        OnPropertyChanged(nameof(CanConfirm));
    }
    
    [RelayCommand]
    private async Task RetryAsync()
    {
        await LoadBranchesAsync();
    }
    
    [RelayCommand]
    private void Confirm()
    {
        if (SelectedBranch == null) return;
        
        try
        {
            // Set branch context (this also persists with DPAPI encryption)
            _tenantContext.SetBranchContext(
                SelectedBranch.Id, 
                SelectedBranch.DisplayName);
            
            Debug.WriteLine($"[BranchSelector] Branch locked: {SelectedBranch.Name} ({SelectedBranch.Id})");
            
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BranchSelector] Failed to set branch context: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
