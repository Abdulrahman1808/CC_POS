using System.Windows.Controls;

namespace POSSystem.Views;

/// <summary>
/// Developer Mode overlay that shows debugging tools.
/// Only visible when the application is running in Developer Mode (DevSecret2026 license).
/// </summary>
public partial class DeveloperOverlay : UserControl
{
    public DeveloperOverlay()
    {
        InitializeComponent();
    }
}
