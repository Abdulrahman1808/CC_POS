using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using System.Windows.Interop;

namespace POSSystem.Services;

/// <summary>
/// Service for handling barcode scanner input using keyboard wedge mode.
/// Barcode scanners typically act as keyboards, sending characters rapidly (<50ms between keys).
/// This service distinguishes scanner input from human typing by measuring input speed.
/// </summary>
public class BarcodeService : IDisposable
{
    private const int RAPID_INPUT_THRESHOLD_MS = 50; // Max ms between keystrokes for scanner
    private const int BARCODE_MIN_LENGTH = 3;
    private const int BARCODE_MAX_LENGTH = 50;
    
    private readonly StringBuilder _barcodeBuffer = new();
    private DateTime _lastKeyTime = DateTime.MinValue;
    private bool _isCapturing = false;
    private IntPtr _hookId = IntPtr.Zero;
    private NativeMethods.LowLevelKeyboardProc? _keyboardProc;
    
    /// <summary>
    /// Event raised when a complete barcode is scanned.
    /// </summary>
    public event EventHandler<string>? BarcodeScanned;
    
    /// <summary>
    /// Indicates if the service is actively listening.
    /// </summary>
    public bool IsListening { get; private set; }
    
    /// <summary>
    /// Start listening for barcode scanner input.
    /// </summary>
    public void StartListening()
    {
        if (IsListening) return;
        
        _keyboardProc = HookCallback;
        _hookId = SetHook(_keyboardProc);
        IsListening = true;
        
        Debug.WriteLine("[BarcodeService] Started listening for scanner input");
    }
    
    /// <summary>
    /// Stop listening for barcode scanner input.
    /// </summary>
    public void StopListening()
    {
        if (!IsListening) return;
        
        NativeMethods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        IsListening = false;
        
        Debug.WriteLine("[BarcodeService] Stopped listening");
    }
    
    private IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL, 
            proc,
            NativeMethods.GetModuleHandle(curModule?.ModuleName ?? ""),
            0);
    }
    
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            ProcessKeyPress(vkCode);
        }
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
    
    private void ProcessKeyPress(int vkCode)
    {
        var now = DateTime.Now;
        var timeSinceLastKey = (now - _lastKeyTime).TotalMilliseconds;
        _lastKeyTime = now;
        
        // Handle Enter key - complete barcode capture
        if (vkCode == 0x0D) // VK_RETURN
        {
            if (_isCapturing && _barcodeBuffer.Length >= BARCODE_MIN_LENGTH)
            {
                var barcode = _barcodeBuffer.ToString();
                Debug.WriteLine($"[BarcodeService] Barcode scanned: {barcode}");
                BarcodeScanned?.Invoke(this, barcode);
            }
            ResetBuffer();
            return;
        }
        
        // Check if this is rapid input (scanner) or slow input (human typing)
        if (timeSinceLastKey > RAPID_INPUT_THRESHOLD_MS && _barcodeBuffer.Length > 0)
        {
            // Too slow - probably human typing, reset
            ResetBuffer();
        }
        
        // Convert virtual key to character
        char? c = VirtualKeyToChar(vkCode);
        if (c.HasValue && (char.IsLetterOrDigit(c.Value) || c.Value == '-'))
        {
            _isCapturing = true;
            _barcodeBuffer.Append(c.Value);
            
            // Prevent buffer overflow
            if (_barcodeBuffer.Length > BARCODE_MAX_LENGTH)
            {
                ResetBuffer();
            }
        }
    }
    
    private char? VirtualKeyToChar(int vkCode)
    {
        // Number keys 0-9
        if (vkCode >= 0x30 && vkCode <= 0x39)
            return (char)vkCode;
        
        // Letter keys A-Z
        if (vkCode >= 0x41 && vkCode <= 0x5A)
            return (char)vkCode;
        
        // Numpad 0-9
        if (vkCode >= 0x60 && vkCode <= 0x69)
            return (char)(vkCode - 0x30);
        
        // Minus/hyphen
        if (vkCode == 0xBD || vkCode == 0x6D)
            return '-';
        
        return null;
    }
    
    private void ResetBuffer()
    {
        _barcodeBuffer.Clear();
        _isCapturing = false;
    }
    
    public void Dispose()
    {
        StopListening();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// P/Invoke declarations for low-level keyboard hook.
/// </summary>
internal static class NativeMethods
{
    public const int WH_KEYBOARD_LL = 13;
    public const int WM_KEYDOWN = 0x0100;
    
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);
}
