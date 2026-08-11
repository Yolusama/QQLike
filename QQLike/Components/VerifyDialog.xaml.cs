using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Entity.VO;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class VerifyDialog : Window
{
    private static VerifyDialog _holderWindow = null;
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

    public static void ShowVerifyDialog(string identifyNum,string source,bool isGroup = false, Func<Task>? confirmCallback=null,Func<Task>? cancelCallback=null)
    {
        if(_holderWindow!=null)
        {
            var holderViewModel = _holderWindow.DataContext as VerifyDialogViewModel;
            if(holderViewModel.Account == identifyNum || holderViewModel.GroupNum == identifyNum)
            {
                _holderWindow.Focus();
                return;
            }
        }
        var dialog = App.ServiceProvider.GetRequiredService<VerifyDialog>();
        var viewModel = (VerifyDialogViewModel)dialog.DataContext;
        viewModel.ConfirmCallback = confirmCallback;
        viewModel.CancelCallback = cancelCallback;
        viewModel.IsGroup = isGroup;
        viewModel.Source = source;
        if(isGroup)
        {
            viewModel.GroupNum = identifyNum;
            viewModel.Title = "申请加入群聊";
        }
        else
        {
            viewModel.Account = identifyNum;
            viewModel.Title = "申请添加好友";
        }

        if (_holderWindow == null)
        {
            _holderWindow = dialog;
            dialog.Show();
        }
        else
        {
            _holderWindow.Close();
            dialog.Show();
            _holderWindow = dialog;
        }
    }

    private void VerifyDialog_OnClosed(object? sender, EventArgs e)
    {
        _holderWindow = null;
    }

    private void VerifyDialog_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 如果点击的是关闭按钮（或其子元素），不触发拖拽
        if (e.OriginalSource is DependencyObject source)
        {
            var parent = source;
            while (parent != null)
            {
                if (parent is Button)
                    return;
                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }
}