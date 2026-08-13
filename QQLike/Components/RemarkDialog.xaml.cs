using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class RemarkDialog : Window
{
    public RemarkDialog(RemarkDialogViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }

    /// <summary>
    /// 打开修改备注的小对话框
    /// </summary>
    public static void ShowRemarkDialog(string contactId, string? remark, Func<string, Task<bool>>? confirmCallback = null)
    {
        var dialog = App.ServiceProvider.GetRequiredService<RemarkDialog>();
        var viewModel = (RemarkDialogViewModel)dialog.DataContext;
        viewModel.ContactId = contactId;
        viewModel.Remark = remark ?? string.Empty;
        viewModel.ConfirmCallback = confirmCallback;

        dialog.Owner = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.IsActive && w != dialog);

        dialog.ShowDialog();
    }

    private void RemarkDialog_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
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
