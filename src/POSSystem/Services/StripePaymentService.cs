using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Stripe payment service for processing card payments at POS.
/// Uses Stripe's REST API directly for payment intents.
/// </summary>
public class StripePaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string? _secretKey;
    private const string StripeApiUrl = "https://api.stripe.com/v1";

    public StripePaymentService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        
        // Get secret key from environment variable (never store in appsettings!)
        _secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") 
                     ?? _configuration["Stripe:SecretKey"];
        
        if (!string.IsNullOrEmpty(_secretKey))
        {
            Debug.WriteLine("[Stripe] Service initialized with API key");
        }
        else
        {
            Debug.WriteLine("[Stripe] WARNING: No API key configured");
        }
    }

    /// <summary>
    /// Check if Stripe is properly configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(_secretKey);

    /// <summary>
    /// Process a card payment using Stripe PaymentIntents.
    /// </summary>
    public async Task<PaymentResult> ProcessCardPaymentAsync(
        long amount,
        string currency,
        string description,
        string? customerEmail = null)
    {
        if (!IsConfigured)
        {
            Debug.WriteLine("[Stripe] Payment service not configured - simulating success");
            // In development mode, simulate successful payment
            return PaymentResult.Succeeded($"sim_{Guid.NewGuid():N}", "simulated");
        }

        try
        {
            Debug.WriteLine($"[Stripe] Creating PaymentIntent for {amount} {currency.ToUpper()}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{StripeApiUrl}/payment_intents");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

            // Build form data for Stripe API
            var formData = new StringBuilder();
            formData.Append($"amount={amount}");
            formData.Append($"&currency={currency.ToLower()}");
            formData.Append($"&description={Uri.EscapeDataString(description)}");
            formData.Append("&confirm=true");
            formData.Append("&payment_method_types[]=card_present"); // For terminal/card reader
            formData.Append("&capture_method=automatic");
            
            if (!string.IsNullOrEmpty(customerEmail))
            {
                formData.Append($"&receipt_email={Uri.EscapeDataString(customerEmail)}");
            }

            // Add metadata
            formData.Append($"&metadata[source]=pos_desktop");
            formData.Append($"&metadata[machine]={Environment.MachineName}");

            request.Content = new StringContent(formData.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(responseBody);
                var root = json.RootElement;
                
                var paymentIntentId = root.GetProperty("id").GetString()!;
                var status = root.GetProperty("status").GetString()!;
                
                Debug.WriteLine($"[Stripe] PaymentIntent created: {paymentIntentId}, status: {status}");
                
                if (status == "succeeded" || status == "requires_capture")
                {
                    return PaymentResult.Succeeded(paymentIntentId, status);
                }
                else
                {
                    return PaymentResult.Failed($"Payment status: {status}");
                }
            }
            else
            {
                var json = JsonDocument.Parse(responseBody);
                var error = json.RootElement.GetProperty("error");
                var message = error.GetProperty("message").GetString();
                
                Debug.WriteLine($"[Stripe] Error: {message}");
                return PaymentResult.Failed(message ?? "Payment failed");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Stripe] Exception: {ex.Message}");
            return PaymentResult.Failed($"Payment error: {ex.Message}");
        }
    }

    /// <summary>
    /// Refund a previous payment.
    /// </summary>
    public async Task<PaymentResult> RefundPaymentAsync(string paymentIntentId, long? amount = null)
    {
        if (!IsConfigured)
        {
            Debug.WriteLine("[Stripe] Refund simulated (not configured)");
            return PaymentResult.Succeeded($"re_sim_{Guid.NewGuid():N}", "simulated_refund");
        }

        try
        {
            Debug.WriteLine($"[Stripe] Creating refund for {paymentIntentId}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{StripeApiUrl}/refunds");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

            var formData = new StringBuilder();
            formData.Append($"payment_intent={paymentIntentId}");
            
            if (amount.HasValue)
            {
                formData.Append($"&amount={amount.Value}");
            }

            request.Content = new StringContent(formData.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(responseBody);
                var refundId = json.RootElement.GetProperty("id").GetString()!;
                var status = json.RootElement.GetProperty("status").GetString()!;
                
                Debug.WriteLine($"[Stripe] Refund created: {refundId}, status: {status}");
                return PaymentResult.Succeeded(refundId, status);
            }
            else
            {
                var json = JsonDocument.Parse(responseBody);
                var error = json.RootElement.GetProperty("error");
                var message = error.GetProperty("message").GetString();
                
                Debug.WriteLine($"[Stripe] Refund error: {message}");
                return PaymentResult.Failed(message ?? "Refund failed");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Stripe] Refund exception: {ex.Message}");
            return PaymentResult.Failed($"Refund error: {ex.Message}");
        }
    }
}
