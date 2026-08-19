using System.ComponentModel;
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

    private void MainView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if(DataContext is MainViewModel viewModel)
        {
            viewModel.ConnectSocketServerCommand.Execute(null);
            viewModel.StartMQConsumingCommand.Execute(null);
        }
    }

    private void MainView_OnClosing(object sender, CancelEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ClosingApplicationCommand.Execute(null);
            viewModel.Dispose();
        }
    }
}