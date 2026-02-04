using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSSystem.Services.Interfaces;

namespace POSSystem.ViewModels;

/// <summary>
/// Main ViewModel that manages navigation between views.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILicenseService _licenseService;
    private readonly IUpdateService _updateService;

    #region Observable Properties

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _currentVersion = "1.0.0";

    [ObservableProperty]
    private string? _newVersion;

    #endregion

    #region Events

    public event EventHandler? LogoutRequested;

    #endregion

    public MainViewModel(
        ILicenseService licenseService,
        IUpdateService updateService)
    {
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));

        CurrentVersion = _updateService.GetCurrentVersion();

        _updateService.UpdateAvailable += OnUpdateAvailable;
    }

    public void SetCurrentView(ObservableObject viewModel)
    {
        CurrentViewModel = viewModel;
    }

    #region Commands

    [RelayCommand]
    private void Logout()
    {
        _licenseService.ClearCache();
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ApplyUpdateAsync()
    {
        await _updateService.ApplyUpdateAsync();
    }

    #endregion

    private void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        IsUpdateAvailable = true;
        NewVersion = e.NewVersion;
    }
}
