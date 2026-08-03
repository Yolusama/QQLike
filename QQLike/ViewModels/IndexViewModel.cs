using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services.Interfaces;
using QQLike.Views;

namespace QQLike.ViewModels;

public partial class IndexViewModel(IWindowFactory windowFactory,
    ILocalStorage localStorage,
    ISessionStorage sessionStorage,
    IProjectLogger logger,
    IHttpService httpService,
    SysSetting setting) : ViewModelBase<Index>
{
    [ObservableProperty] private string _userAccount;
    [ObservableProperty] private string _password;
    [ObservableProperty] private bool _autoLogin;
    [ObservableProperty] private bool _rememberPassword;

    private Window _registerWindow = null;
    
    public void InitLoginSetting()
    {
        var currentUser = localStorage.Get<string>(CachingKeys.CurrentUser);
        var rememberPassword = localStorage.Get<bool>(nameof(RememberPassword));
        var autoLogin = localStorage.Get<bool>(nameof(AutoLogin));
        if (rememberPassword)
        {
            View.PasswordBox.Password = localStorage.Get<string>(currentUser);
            UserAccount = currentUser;
            RememberPassword =  rememberPassword;
        }

        if (autoLogin && rememberPassword)
        {
            AutoLogin = autoLogin;
            LoginCommand.Execute(null);
        }
        
    }


    [RelayCommand]
    private async Task Login()
    {
        var loading = LoadingComponent.Loading(View,"正在登录...");
        try
        {
            var loginModel = new UserLoginModel
            {
                UserAccount = UserAccount,
                Password = View.PasswordBox.Password.ToSha256Str()
            };
            var apiUrl = $"{setting.ApiUrl}/api/User/Login";
            var json = JsonSerializer.Serialize(loginModel);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var resultStr = await httpService.Request(apiUrl, HttpMethod.Post, httpContent);
            var res = JsonSerializer.Deserialize<ResponseResult<UserLoginVO>>(resultStr,
                Constants.DesSerializerOptions);
            if (res.Success)
            {
                sessionStorage.Set(CachingKeys.User, res.Data);
                var userAccounts = localStorage.Get<List<string>>(CachingKeys.UserAccounts) ?? [];
                if (!userAccounts.Contains(UserAccount))
                {
                    userAccounts.Add(UserAccount);
                    localStorage.Set(CachingKeys.UserAccounts, userAccounts);
                }

                if (RememberPassword)
                {
                    localStorage.Set(UserAccount, View.PasswordBox.Password);
                    localStorage.Set(nameof(RememberPassword), true);
                    localStorage.Set(CachingKeys.CurrentUser, UserAccount);
                }
                else
                {
                    localStorage.Remove(nameof(RememberPassword));
                    var currentUser = localStorage.Get<string>(CachingKeys.CurrentUser);
                    localStorage.Remove(currentUser);
                }

                if (AutoLogin)
                    localStorage.Set(nameof(AutoLogin), true);
                else
                    localStorage.Remove(nameof(AutoLogin));
                await Task.Delay(3000);
                loading.Complete();
                if(loading.Cancelled)
                    return;
                var window = windowFactory.GetWindow<MainView>();
                window.Show();
                View.Close();
            }
            else
            {
                await logger.LogAsync($"登录失败:{res.Message}", "用户登录");
                MessageComponent.ShowMessage(View,$"登录失败，原因:{res.Message}", MessageType.Error);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"登录过程中程序出现异常:{e}", "用户登录");
            MessageComponent.ShowMessage(View, $"登录过程中发生异常:{e.Message}，请稍后重试", MessageType.Error);
        }
    }
    
    [RelayCommand]
    private void GoRegister()
    {
        if (_registerWindow == null)
        {
            var window = windowFactory.GetWindow<RegisterView>(View);
            _registerWindow = window;
            window.Closing += (sender, args) =>
            {
                if(localStorage.KeyExists(CachingKeys.RegisteredAccount))
                    UserAccount = localStorage.Get<string>(CachingKeys.RegisteredAccount);
            };
            window.Closed += (sender, args) =>
            {
                _registerWindow = null;
            };
            window.Show();
        }
    }
}