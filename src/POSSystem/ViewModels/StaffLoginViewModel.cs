using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSSystem.Services.Interfaces;

namespace POSSystem.ViewModels;

/// <summary>
/// ViewModel for the PIN-based staff login screen.
/// </summary>
public partial class StaffLoginViewModel : ObservableObject
{
    private readonly IStaffService _staffService;
    private readonly StringBuilder _pinBuilder = new();

    [ObservableProperty]
    private string _pinMasked = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public StaffLoginViewModel(IStaffService staffService)
    {
        _staffService = staffService;
    }

    [RelayCommand]
    private void AppendPin(string digit)
    {
        ErrorMessage = string.Empty;
        
        if (_pinBuilder.Length < 6)
        {
            _pinBuilder.Append(digit);
            UpdateMaskedPin();
        }
    }

    [RelayCommand]
    private void Backspace()
    {
        ErrorMessage = string.Empty;
        
        if (_pinBuilder.Length > 0)
        {
            _pinBuilder.Length--;
            UpdateMaskedPin();
        }
    }

    [RelayCommand]
    private void Clear()
    {
        ErrorMessage = string.Empty;
        _pinBuilder.Clear();
        UpdateMaskedPin();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (_pinBuilder.Length < 4)
        {
            ErrorMessage = "PIN must be at least 4 digits";
            return;
        }

        var pin = _pinBuilder.ToString();
        var success = await _staffService.AuthenticateByPinAsync(pin);

        if (success)
        {
            // Clear PIN for next time
            _pinBuilder.Clear();
            UpdateMaskedPin();
            ErrorMessage = string.Empty;
        }
        else
        {
            ErrorMessage = "Invalid PIN";
            _pinBuilder.Clear();
            UpdateMaskedPin();
        }
    }

    private void UpdateMaskedPin()
    {
        PinMasked = new string('●', _pinBuilder.Length);
    }
}
