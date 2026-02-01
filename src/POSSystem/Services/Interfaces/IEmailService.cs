using System.Threading.Tasks;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Interface for email notification service.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email notification to the admin about transaction clearing.
    /// </summary>
    /// <param name="triggeredBy">Who triggered the action (e.g., "Developer Mode")</param>
    /// <param name="transactionCount">Number of transactions cleared</param>
    /// <param name="totalSalesCleared">Total sales amount cleared</param>
    /// <returns>True if email was sent or logged successfully</returns>
    Task<bool> SendTransactionClearNotificationAsync(
        string triggeredBy,
        int transactionCount,
        decimal totalSalesCleared);
    
    /// <summary>
    /// Sends a generic admin notification email.
    /// </summary>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML supported)</param>
    /// <returns>True if email was sent or logged successfully</returns>
    Task<bool> SendAdminNotificationAsync(string subject, string body);
}
