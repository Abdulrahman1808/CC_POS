using System;
using System.IO;
using System.Text;

namespace POSSystem.Helpers;

/// <summary>
/// Helper for generating Egyptian Tax Authority (ETA) compliant QR codes.
/// Implements TLV (Tag-Length-Value) encoding per ZATCA/ETA e-invoice specifications.
/// </summary>
/// <remarks>
/// The TLV format encodes 5 mandatory fields:
/// Tag 1: Seller Name (UTF-8)
/// Tag 2: Tax Registration Number (UTF-8)
/// Tag 3: Timestamp (ISO 8601 format)
/// Tag 4: Total Amount (decimal string with 2 decimal places)
/// Tag 5: VAT Amount (decimal string with 2 decimal places)
/// 
/// The resulting byte array is Base64-encoded for QR code generation.
/// </remarks>
public static class EtaQrCodeHelper
{
    /// <summary>
    /// Generates a Base64-encoded TLV string for ETA-compliant QR codes.
    /// </summary>
    /// <param name="sellerName">Store/business name</param>
    /// <param name="taxRegNumber">Egyptian Tax Registration Number</param>
    /// <param name="timestamp">Transaction timestamp</param>
    /// <param name="totalAmount">Total invoice amount including VAT</param>
    /// <param name="vatAmount">VAT amount</param>
    /// <returns>Base64-encoded TLV byte array</returns>
    /// <exception cref="ArgumentException">Thrown when required fields are empty</exception>
    public static string GenerateTlvBase64(
        string sellerName,
        string taxRegNumber,
        DateTime timestamp,
        decimal totalAmount,
        decimal vatAmount)
    {
        if (string.IsNullOrWhiteSpace(sellerName))
            throw new ArgumentException("Seller name is required", nameof(sellerName));
        
        if (string.IsNullOrWhiteSpace(taxRegNumber))
            throw new ArgumentException("Tax registration number is required", nameof(taxRegNumber));
        
        using var ms = new MemoryStream();
        
        // Tag 1: Seller Name
        WriteTlv(ms, 0x01, sellerName);
        
        // Tag 2: Tax Registration Number
        WriteTlv(ms, 0x02, taxRegNumber);
        
        // Tag 3: Timestamp in ISO 8601 format
        WriteTlv(ms, 0x03, timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        
        // Tag 4: Total Amount (2 decimal places)
        WriteTlv(ms, 0x04, totalAmount.ToString("F2"));
        
        // Tag 5: VAT Amount (2 decimal places)
        WriteTlv(ms, 0x05, vatAmount.ToString("F2"));
        
        return Convert.ToBase64String(ms.ToArray());
    }
    
    /// <summary>
    /// Generates TLV Base64 from a ReceiptModel.
    /// </summary>
    public static string GenerateTlvBase64(Models.ReceiptModel receipt)
    {
        return GenerateTlvBase64(
            receipt.StoreName,
            receipt.TaxRegistrationNumber,
            receipt.Timestamp,
            receipt.Total,
            receipt.VatAmount
        );
    }
    
    /// <summary>
    /// Writes a single TLV (Tag-Length-Value) entry to the stream.
    /// </summary>
    /// <param name="stream">Output stream</param>
    /// <param name="tag">Tag byte (1-5)</param>
    /// <param name="value">String value to encode</param>
    private static void WriteTlv(MemoryStream stream, byte tag, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        
        // Validate length fits in single byte (max 255)
        if (bytes.Length > 255)
        {
            throw new ArgumentException($"TLV value for tag {tag} exceeds 255 bytes");
        }
        
        // Write Tag
        stream.WriteByte(tag);
        
        // Write Length
        stream.WriteByte((byte)bytes.Length);
        
        // Write Value
        stream.Write(bytes, 0, bytes.Length);
    }
    
    /// <summary>
    /// Decodes a TLV Base64 string for verification/debugging.
    /// </summary>
    /// <param name="tlvBase64">Base64-encoded TLV string</param>
    /// <returns>Dictionary of tag numbers to decoded values</returns>
    public static Dictionary<int, string> DecodeTlvBase64(string tlvBase64)
    {
        var result = new Dictionary<int, string>();
        var bytes = Convert.FromBase64String(tlvBase64);
        
        int i = 0;
        while (i < bytes.Length)
        {
            if (i + 2 > bytes.Length) break;
            
            var tag = bytes[i];
            var length = bytes[i + 1];
            
            if (i + 2 + length > bytes.Length) break;
            
            var value = Encoding.UTF8.GetString(bytes, i + 2, length);
            result[tag] = value;
            
            i += 2 + length;
        }
        
        return result;
    }
}
