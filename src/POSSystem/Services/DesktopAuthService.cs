using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using POSSystem.Models;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Handles Supabase email/password authentication for Desktop app.
/// Pairs the local MachineID with the user's BusinessId.
/// </summary>
public interface IAuthService
{
    Task<AuthResult> SignInAsync(string email, string password);
    Task<AuthResult> SignUpAsync(string email, string password, string businessName);
    Task SignOutAsync();
    bool IsAuthenticated { get; }
    string? AccessToken { get; }
    Guid? UserId { get; }
}

public class DesktopAuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ISettingsService _settingsService;
    private readonly ILicenseService _licenseService;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    private AuthSession? _session;

    public bool IsAuthenticated => _session != null && !string.IsNullOrEmpty(_session.AccessToken);
    public string? AccessToken => _session?.AccessToken;
    public Guid? UserId => _session?.User?.Id;

    public DesktopAuthService(
        HttpClient httpClient,
        IConfiguration configuration,
        ISettingsService settingsService,
        ILicenseService licenseService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _settingsService = settingsService;
        _licenseService = licenseService;

        _supabaseUrl = configuration["Supabase:Url"] ?? "";
        _supabaseKey = configuration["Supabase:ApiKey"] ?? "";
    }

    /// <summary>
    /// Signs in with email/password and pairs MachineID to BusinessId.
    /// </summary>
    public async Task<AuthResult> SignInAsync(string email, string password)
    {
        if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_supabaseKey))
        {
            return new AuthResult { Success = false, ErrorMessage = "Supabase not configured" };
        }

        try
        {
            Debug.WriteLine($"[Auth] Signing in: {email}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/auth/v1/token?grant_type=password")
            {
                Content = JsonContent.Create(new { email, password })
            };
            request.Headers.Add("apikey", _supabaseKey);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<SupabaseError>(json);
                Debug.WriteLine($"[Auth] Sign in failed: {error?.Message}");
                return new AuthResult { Success = false, ErrorMessage = error?.Message ?? "Authentication failed" };
            }

            _session = JsonSerializer.Deserialize<AuthSession>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Debug.WriteLine($"[Auth] Signed in as: {_session?.User?.Email}");

            // Link machine to business
            await LinkMachineToBusinessAsync();

            return new AuthResult { Success = true, Session = _session };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth] Error: {ex.Message}");
            return new AuthResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Signs up new user and creates their business profile.
    /// </summary>
    public async Task<AuthResult> SignUpAsync(string email, string password, string businessName)
    {
        if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_supabaseKey))
        {
            return new AuthResult { Success = false, ErrorMessage = "Supabase not configured" };
        }

        try
        {
            Debug.WriteLine($"[Auth] Signing up: {email}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/auth/v1/signup")
            {
                Content = JsonContent.Create(new { email, password })
            };
            request.Headers.Add("apikey", _supabaseKey);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<SupabaseError>(json);
                return new AuthResult { Success = false, ErrorMessage = error?.Message ?? "Signup failed" };
            }

            _session = JsonSerializer.Deserialize<AuthSession>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Create business profile
            if (_session?.User != null)
            {
                var profile = new BusinessProfile
                {
                    OwnerId = _session.User.Id,
                    MachineId = _licenseService.GetMachineId(),
                    BusinessName = businessName
                };
                await _settingsService.SaveBusinessProfileAsync(profile);
            }

            return new AuthResult { Success = true, Session = _session };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth] Signup error: {ex.Message}");
            return new AuthResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Links the current MachineID to the user's business in Supabase.
    /// </summary>
    private async Task LinkMachineToBusinessAsync()
    {
        if (_session?.User == null || string.IsNullOrEmpty(_session.AccessToken))
            return;

        try
        {
            var machineId = _licenseService.GetMachineId();
            Debug.WriteLine($"[Auth] Linking machine {machineId[..8]}... to user");

            // Call RPC function to link machine
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/rpc/link_machine_to_owner")
            {
                Content = JsonContent.Create(new { p_machine_id = machineId })
            };
            request.Headers.Add("apikey", _supabaseKey);
            request.Headers.Add("Authorization", $"Bearer {_session.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode && Guid.TryParse(json.Trim('"'), out var businessId))
            {
                await _settingsService.SetBusinessIdAsync(businessId);
                Debug.WriteLine($"[Auth] ✓ Machine linked to business: {businessId}");

                // Fetch and cache business profile
                await FetchBusinessProfileAsync(businessId);
            }
            else
            {
                Debug.WriteLine($"[Auth] Link failed: {json}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth] Link error: {ex.Message}");
        }
    }

    private async Task FetchBusinessProfileAsync(Guid businessId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_supabaseUrl}/rest/v1/business_profiles?id=eq.{businessId}&select=*");
            request.Headers.Add("apikey", _supabaseKey);
            request.Headers.Add("Authorization", $"Bearer {_session!.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            var profiles = JsonSerializer.Deserialize<BusinessProfile[]>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (profiles?.Length > 0)
            {
                await _settingsService.SaveBusinessProfileAsync(profiles[0]);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth] Fetch profile error: {ex.Message}");
        }
    }

    public Task SignOutAsync()
    {
        _session = null;
        Debug.WriteLine("[Auth] Signed out");
        return Task.CompletedTask;
    }
}

#region Auth Models

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public AuthSession? Session { get; set; }
}

public class AuthSession
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("user")]
    public AuthUser? User { get; set; }
}

public class AuthUser
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class SupabaseError
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error_description")]
    public string? Description { get; set; }
}

#endregion
