using System.Threading.Tasks;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Interface for payment processing services (Stripe, etc.)
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Process a card payment for a POS transaction.
    /// </summary>
    /// <param name="amount">Amount in smallest currency unit (e.g., cents/piastres)</param>
    /// <param name="currency">Currency code (e.g., "egp", "usd")</param>
    /// <param name="description">Transaction description</param>
    /// <param name="customerEmail">Optional customer email for receipt</param>
    /// <returns>Payment result with success status and transaction ID</returns>
    Task<PaymentResult> ProcessCardPaymentAsync(
        long amount,
        string currency,
        string description,
        string? customerEmail = null);
    
    /// <summary>
    /// Refund a previous payment.
    /// </summary>
    /// <param name="paymentIntentId">The Stripe PaymentIntent ID to refund</param>
    /// <param name="amount">Amount to refund (null for full refund)</param>
    /// <returns>Refund result</returns>
    Task<PaymentResult> RefundPaymentAsync(string paymentIntentId, long? amount = null);
    
    /// <summary>
    /// Check if payment service is configured and available.
    /// </summary>
    bool IsConfigured { get; }
}

/// <summary>
/// Result of a payment operation.
/// </summary>
public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Status { get; set; }
    
    public static PaymentResult Succeeded(string transactionId, string status = "succeeded") 
        => new() { Success = true, TransactionId = transactionId, Status = status };
    
    public static PaymentResult Failed(string errorMessage) 
        => new() { Success = false, ErrorMessage = errorMessage };
}
