using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Views;

namespace QQLike.ViewModels;

public partial class EntryHeaderViewModel : ViewModelBase<EntryHeader>
{
    [RelayCommand]
    private void Exit()
    {
        var window = Window.GetWindow(View);
        window.Close();
    }

    [RelayCommand]
    private void Minimize()
    {
        var window = Window.GetWindow(View);
        window.WindowState = WindowState.Minimized;
    }
}