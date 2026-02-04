using System.Windows;

namespace POSSystem.Views;

/// <summary>
/// Branch selection dialog shown after license activation.
/// Allows user to select and lock a branch to this machine.
/// </summary>
public partial class BranchSelectorView : Window
{
    public BranchSelectorView()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Sets the ViewModel and wires up close events.
    /// </summary>
    public void SetViewModel(ViewModels.BranchSelectorViewModel viewModel)
    {
        DataContext = viewModel;
        
        // Wire up close events
        viewModel.RequestClose += (confirmed) =>
        {
            DialogResult = confirmed;
            Close();
        };
    }
}
