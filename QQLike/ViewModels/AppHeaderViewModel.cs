using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using QQLike.Components;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Services.Interfaces;
using QQLike.Views.User;

namespace QQLike.ViewModels;

public partial class AppHeaderViewModel : ViewModelBase<AppHeader>
{
    [ObservableProperty]
    private string _userAvatar;
    [ObservableProperty]
    private string _nickname;
    [ObservableProperty]
    private string _signature;
    
    private bool _maximized;
    private readonly SysSetting setting;
    private readonly ISessionStorage sessionStorage;
    private readonly IWindowFactory windowFactory;
    
    public AppHeaderViewModel(SysSetting setting, ISessionStorage sessionStorage, IWindowFactory windowFactory)
    {
        this.setting = setting;
        this.sessionStorage = sessionStorage;
        this.windowFactory = windowFactory;
        SetUserInfo();
    }

    private void SetUserInfo()
    {
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);

        UserAvatar = $"{setting.ApiUrl}/Files/Images/{user.Avatar}";
        Nickname = user.Nickname;
        Signature = user.Signature;
    }

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
        var index = windowFactory.GetWindow<Index>();
        index.Close();
    }
    
    [RelayCommand]
    private void OpenUserProfileCommand()
    {
      
    }
}