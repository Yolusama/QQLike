using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Entity.VO;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class VerifyDialog : Window
{
    public VerifyDialog(VerifyDialogViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }
    
    private void VerifyDialog_OnLoadedLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is VerifyDialogViewModel viewModel)
        {
            viewModel.LoadUserVerifyInfoCommand.Execute(null);
        }
    }

    public static void ShowVerifyDialog(Window owner,string identifyNum,string source,bool isGroup = false, Func<Task>? confirmCallback=null,Func<Task>? cancelCallback=null)
    {
        var dialog = App.ServiceProvider.GetRequiredService<VerifyDialog>();
        dialog.Owner = owner;
        var viewModel = (VerifyDialogViewModel)dialog.DataContext;
        viewModel.ConfirmCallback = confirmCallback;
        viewModel.CancelCallback = cancelCallback;
        viewModel.IsGroup = isGroup;
        viewModel.Source = source;
        if(isGroup)
        {
            viewModel.Account = identifyNum;
            viewModel.Title = "申请加入群聊";
        }
        else
        {
            viewModel.GroupNum = identifyNum;
            viewModel.Title = "申请添加好友";
        }
        dialog.Show();
    }
}