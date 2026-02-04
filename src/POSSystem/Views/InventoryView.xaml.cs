using System.Windows.Controls;
using POSSystem.ViewModels;

namespace POSSystem.Views;

public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
    }

    public void SetViewModel(InventoryViewModel viewModel)
    {
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}
