using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Entity.VO;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class VerifyDialog : Window
{
    public VerifyDialog()
    {
        InitializeComponent();
    }
    
    private void VerifyDialog_OnLoadedLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is VerifyDialogViewModel viewModel)
        {
            viewModel.LoadUserVerifyInfoCommand.Execute(null);
        }
    }

    public static void ShowVerifyDialog(Window owner,string source,bool isGroup = false, Func<Task>? confirmCallback=null,Func<Task>? cancelCallback=null)
    {
        var dialog = App.ServiceProvider.GetRequiredService<VerifyDialog>();
        dialog.Owner = owner;
        var viewModel = (VerifyDialogViewModel)dialog.DataContext;
        viewModel.ConfirmCallback = confirmCallback;
        viewModel.CancelCallback = cancelCallback;
        viewModel.IsGroup = isGroup;
        viewModel.Source = source;
        dialog.Show();
    }
}