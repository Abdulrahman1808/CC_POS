using System.Windows.Controls;
using POSSystem.ViewModels;

namespace POSSystem.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
    }

    public void SetViewModel(ReportsViewModel viewModel)
    {
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}
