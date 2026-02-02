using System;
using System.Threading.Tasks;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Interface for payment processing services.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Processes a single payment transaction.
    /// </summary>
    Task<PaymentResult> ProcessPaymentAsync(decimal amount, string currency, string paymentMethodId);

    /// <summary>
    /// Creates a checkout session for subscriptions or one-time payments.
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(string planId, string customerEmail);
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
