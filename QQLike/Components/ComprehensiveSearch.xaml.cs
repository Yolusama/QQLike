using System.Windows;
using System.Windows.Input;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class ComprehensiveSearch : Window
{
    private static ComprehensiveSearch _holderWindow = null;
    public ComprehensiveSearch(ComprehensiveSearchViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    public new void Show()
    {
        if (_holderWindow == null)
        {
            base.Show();
            _holderWindow = this;
        }
        else
            _holderWindow.Focus();
    }

    private void ComprehensiveSearch_OnClosed(object? sender, EventArgs e)
    {
        _holderWindow = null;
    }
}