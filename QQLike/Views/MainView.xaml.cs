using System.Windows;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Views;

public partial class MainView : Window
{
    public MainView(MainViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }
}