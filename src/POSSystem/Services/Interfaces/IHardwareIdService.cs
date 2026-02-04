using System.Threading.Tasks;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Service for generating unique hardware-based machine identifiers.
/// Used for licensing and device identification.
/// </summary>
public interface IHardwareIdService
{
    /// <summary>
    /// Gets a unique machine identifier based on hardware components.
    /// Combines CPU ID, Motherboard serial, and MAC address.
    /// </summary>
    /// <returns>Unique machine ID as uppercase string</returns>
    string GetMachineId();
    
    /// <summary>
    /// Gets the raw CPU processor ID.
    /// </summary>
    string GetCpuId();
    
    /// <summary>
    /// Gets the motherboard serial number.
    /// </summary>
    string GetMotherboardSerial();
    
    /// <summary>
    /// Gets the primary MAC address.
    /// </summary>
    string GetMacAddress();
}
