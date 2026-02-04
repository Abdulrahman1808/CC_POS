using System;

namespace POSSystem.Helpers;

/// <summary>
/// Helper for generating Egypt Tax Authority (ETA) E-Receipt data.
/// </summary>
public static class EtaReceiptHelper
{
    /// <summary>
    /// Generates a placeholder QR code string for Egypt E-Receipts.
    /// This will be linked to the actual ETA API in production.
    /// </summary>
    /// <param name="transactionId">Unique transaction ID</param>
    /// <param name="taxId">Store Tax ID (from StoreSettings)</param>
    /// <param name="total">Transaction total in EGP</param>
    /// <param name="timestamp">Transaction timestamp</param>
    /// <returns>QR code string ready for encoding</returns>
    public static string GenerateQrCodeString(
        Guid transactionId,
        string taxId,
        decimal total,
        DateTime timestamp)
    {
        // ETA QR format placeholder - actual format TBD by Egyptian Tax Authority
        // Format: TaxID|TransactionID|Total|Timestamp|Checksum
        
        var qrData = new[]
        {
            taxId.Replace("-", ""),                     // Tax ID (digits only)
            transactionId.ToString("N"),                // Transaction ID (no dashes)
            total.ToString("F2"),                       // Total with 2 decimals
            timestamp.ToString("yyyyMMddHHmmss"),       // ISO compact timestamp
            GenerateChecksum(transactionId, total)      // Simple checksum
        };

        return string.Join("|", qrData);
    }

    /// <summary>
    /// Generate a simple checksum for receipt verification.
    /// </summary>
    private static string GenerateChecksum(Guid transactionId, decimal total)
    {
        // Simple hash for demo - production would use cryptographic signature
        var input = $"{transactionId}{total:F2}";
        var hash = input.GetHashCode();
        return Math.Abs(hash).ToString("X8");
    }

    /// <summary>
    /// Validates an Egyptian Tax ID format.
    /// Format: ###-###-### or 9 digits.
    /// </summary>
    public static bool IsValidTaxId(string taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return false;

        // Remove dashes and check for 9 digits
        var digits = taxId.Replace("-", "");
        return digits.Length == 9 && long.TryParse(digits, out _);
    }

    /// <summary>
    /// Formats an Egyptian Tax ID with dashes.
    /// </summary>
    public static string FormatTaxId(string taxId)
    {
        var digits = taxId.Replace("-", "");
        if (digits.Length != 9)
            return taxId;

        return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 3)}";
    }
}
