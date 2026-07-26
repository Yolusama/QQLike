using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.DTO;
using QQLike.Functional.Instructure;

namespace QQLike.ViewModels;

public partial class AppHeaderViewModel : ViewModelBase<AppHeader>
{
    [ObservableProperty]
    private string _userAvatar;
    [ObservableProperty]
    private string _nickname;
    [ObservableProperty]
    private string _signature;
    
    private readonly SysSetting setting;
    private readonly ISessionStorage sessionStorage;
    
    public AppHeaderViewModel(SysSetting setting, ISessionStorage sessionStorage)
    {
        this.setting = setting;
        this.sessionStorage = sessionStorage;
        var user = sessionStorage.Get<UserLoginDTO>(CachingKeys.User);

        UserAvatar = $"{setting.ApiUrl}/Files/Image/{user.Avatar}";
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
        window.WindowState = WindowState.Maximized;
    }
    
    [RelayCommand]
    private void Exit()
    {
        var window = Window.GetWindow(View);
        window.Close();
    }
}