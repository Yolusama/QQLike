using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;

namespace QQLike.ViewModels;

public partial class RemarkDialogViewModel : ViewModelBase<RemarkDialog>
{
    [ObservableProperty]
    private string _remark = string.Empty;

    /// <summary>
    /// 当前查看的联系人ID
    /// </summary>
    public string ContactId { get; set; }

    /// <summary>
    /// 确认修改后的回调，入参为新的备注内容，返回是否修改成功
    /// </summary>
    public Func<string, Task<bool>>? ConfirmCallback { get; set; }

    [RelayCommand]
    private async Task Confirm()
    {
        if (ConfirmCallback != null)
        {
            var success = await ConfirmCallback.Invoke(Remark);
            if (!success)
                return;
        }

        View.Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        View.Close();
    }

    [RelayCommand]
    private void Close()
    {
        View.Close();
    }
}
