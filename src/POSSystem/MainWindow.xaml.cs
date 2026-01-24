using System.Reflection;
using System.Windows;
using POSSystem.ViewModels;

namespace POSSystem;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetDynamicTitle();
    }

    public void SetViewModel(MainViewModel viewModel)
    {
        DataContext = viewModel;
    }

    /// <summary>
    /// Sets the window title dynamically from assembly info.
    /// Format: "[App Name] - Professional Edition v1.0.0"
    /// </summary>
    private void SetDynamicTitle()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var versionString = version != null 
            ? $"v{version.Major}.{version.Minor}.{version.Build}"
            : "v1.0.0";

        Title = $"POS System - Professional Edition {versionString}";
    }
}