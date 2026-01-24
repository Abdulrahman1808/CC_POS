using System.Windows.Controls;
using POSSystem.ViewModels;

namespace POSSystem.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public void SetViewModel(StoreSettingsViewModel viewModel)
    {
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}
