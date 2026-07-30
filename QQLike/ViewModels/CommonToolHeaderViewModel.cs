using System.Windows;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using QQLike.Components;
using QQLike.Services.Interfaces;

namespace QQLike.ViewModels;

public partial class CommonToolHeaderViewModel(IWindowFactory windowFactory) : ViewModelBase<CommonToolHeader>
{
    private bool _maximized;

    [RelayCommand]
    private void Minimize()
    {
        var window = Window.GetWindow(View);
        window.WindowState = WindowState.Minimized;
    }

    [RelayCommand]
    private void Maximize()
    {
        var window = Window.GetWindow(View);
        if(!_maximized)
        {
            window.WindowState = WindowState.Maximized;
            View.MaximizeIcon.Kind = PackIconKind.WindowRestore;
        }
        else
        {
            window.WindowState = WindowState.Normal;
            View.MaximizeIcon.Kind = PackIconKind.WindowMaximize;
        }
        _maximized = !_maximized;
    }
    
    [RelayCommand]
    private void Exit()
    {
        var window = Window.GetWindow(View);
        window.Close();
    }
}