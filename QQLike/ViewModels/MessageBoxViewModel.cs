using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;

namespace QQLike.ViewModels;

public partial class MessageBoxViewModel : ViewModelBase<MessageBoxComponent>
{
    [ObservableProperty]
    private string _message;
    [ObservableProperty]
    private string _title;
    [ObservableProperty]
    private string _confirmButtonText;
    [ObservableProperty]
    private string _cancelButtonText;
    
    private bool _cancelled = false;
    
    public Action? ConfirmAction { get; set; }
    public Action<string>? CancelAction { get; set; }

    [RelayCommand]
    private void CloseCommand()
    {
        View.Close();
    }

    [RelayCommand]
    private void ConfirmCommand()
    {
        ConfirmAction?.Invoke();
        View.Close();
    }

    [RelayCommand]
    private void CancelCommand()
    {
        _cancelled = true;
        CancelAction?.Invoke(_cancelled ? "取消" : "关闭");
        View.Close();
    }
}