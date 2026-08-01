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
        foreach (Window window in Application.Current.Windows)
            {
                try
                {
                   window.Close();
                }
                catch
                {
                    // ignored
                }
            }
        Application.Current.Shutdown();
    }

    [RelayCommand]
    private void Minimize()
    {
        var window = Window.GetWindow(View);
        window.WindowState = WindowState.Minimized;
    }
}