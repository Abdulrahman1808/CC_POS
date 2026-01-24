using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace POSSystem.Services;

/// <summary>
/// Service for sending email notifications to admin.
/// </summary>
public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly string _logPath;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _logPath = Path.Combine(AppContext.BaseDirectory, "email_log.txt");
    }

    /// <summary>
    /// Sends an email notification to the admin about transaction clearing.
    /// </summary>
    public async Task<bool> SendTransactionClearNotificationAsync(
        string triggeredBy,
        int transactionCount,
        decimal totalSalesCleared)
    {
        try
        {
            var adminEmail = _configuration["Admin:Email"] ?? "abdulrahman.mohamed1808@gmail.com";
            var smtpHost = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var smtpUser = _configuration["Smtp:Username"] ?? "";
            var smtpPass = _configuration["Smtp:Password"] ?? "";
            var fromEmail = _configuration["Smtp:FromEmail"] ?? "noreply@possystem.local";
            var fromName = _configuration["Smtp:FromName"] ?? "POS System";

            var subject = "⚠️ POS System Alert: Transactions Cleared";
            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #DC2626;'>⚠️ Transaction Clearing Alert</h2>
    <p>The following action was performed on your POS System:</p>
    
    <table style='border-collapse: collapse; margin: 20px 0;'>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Action</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>All Transactions Cleared</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Triggered By</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>{triggeredBy}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Transactions Deleted</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>{transactionCount}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Total Sales Cleared</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>${totalSalesCleared:F2}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Timestamp</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Machine Name</td>
            <td style='padding: 10px; border: 1px solid #ddd;'>{Environment.MachineName}</td>
        </tr>
    </table>
    
    <p style='color: #666;'>This is an automated notification from POS System.</p>
</body>
</html>";

            // Check if SMTP credentials are configured
            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
            {
                // Log to file instead if SMTP not configured
                await LogEmailToFileAsync(subject, body, adminEmail);
                Debug.WriteLine("[Email] SMTP not configured - logged to file instead");
                return true;
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                Timeout = 10000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(adminEmail);

            await client.SendMailAsync(message);
            Debug.WriteLine($"[Email] Notification sent to {adminEmail}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Email] Failed to send: {ex.Message}");
            
            // Log the failed email attempt
            await LogEmailToFileAsync(
                "FAILED EMAIL",
                $"Error: {ex.Message}\nTriggered By: {triggeredBy}\nTransactions: {transactionCount}",
                _configuration["Admin:Email"] ?? "unknown"
            );
            
            return false;
        }
    }

    private async Task LogEmailToFileAsync(string subject, string body, string recipient)
    {
        try
        {
            var logEntry = $@"
================================================================================
Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
To: {recipient}
Subject: {subject}
--------------------------------------------------------------------------------
{body}
================================================================================

";
            await File.AppendAllTextAsync(_logPath, logEntry);
            Debug.WriteLine($"[Email] Logged to {_logPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Email] Failed to log: {ex.Message}");
        }
    }
}
