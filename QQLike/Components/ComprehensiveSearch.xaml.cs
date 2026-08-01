using System.Windows;
using System.Windows.Input;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class ComprehensiveSearch : Window
{
    public ComprehensiveSearch(ComprehensiveSearchViewModel  viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}