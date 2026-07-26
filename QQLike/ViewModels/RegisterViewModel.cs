using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.DTO;
using QQLike.Entity.Model;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services.Interfaces;
using QQLike.Views;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class RegisterViewModel(IEmailSender emailSender
    ,IRandomGenerator generator
    ,IRedisCache redis
    ,ISqlSugarClient sugar
    ,IHttpService  httpService
    ,ILocalStorage localStorage
    ,IProjectLogger logger
    ,IWindowFactory windowFactory,
    SysSetting setting) : ViewModelBase<RegisterView>
{
    [ObservableProperty]
    private string _nickname;
    [ObservableProperty]
    private string _password;
    [ObservableProperty]
    private string _email;
    [ObservableProperty]
    private bool _minimizeButtonVisible;
    [ObservableProperty]
    private bool _verifyEnabled = true;
    [ObservableProperty]
    private string _verificationCode;
    [ObservableProperty]
    private string _verifyButtonContent = "获取验证码";

    [RelayCommand]
    private async Task GetVerificationCode()
    {
        if (!VerifyEnabled)
            return;

        var key = $"{CachingKeys.VerificationCode}_{_email}";
        try
        {
            var verificationCode = generator.GenerateByNumbers(Constants.RegisterCodeLength);
            var html = @$"<html>
                           <body><p>您当前获取的验证码:<strong>{verificationCode}</strong>,请于5分钟内使用</p></body>
                         </html>";
            await emailSender.SendAsync(_email, "QQLike验证码", html);
            await redis.SetAsync(key, verificationCode, TimeSpan.FromMinutes(5));
            VerifyEnabled = false;

            for (var seconds = 60; seconds > 0; seconds--)
            {
                VerifyButtonContent = $"{seconds}秒";
                await Task.Delay(1000);
            }

            VerifyButtonContent = "获取验证码";
            VerifyEnabled = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            VerifyEnabled  = true;
            VerifyButtonContent = "获取验证码";
            await logger.LogAsync($"验证码发送失败:{e.Message}", "用户注册");
            await redis.RemoveAsync(key);
            MessageBox.Show("验证码发送失败，请稍后重试", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        
    }

    [RelayCommand]
    private void ReturnToLogin()
    {
        View.Close();
    }

    [RelayCommand]
    private async Task Register()
    {
        if(await sugar.Queryable<User>().Where(e => e.Email == _email).AnyAsync())
        {
            MessageBox.Show("该邮箱已被注册，请使用其他邮箱", "注册失败", MessageBoxButton.OK, 
                MessageBoxImage.Warning);
            return;
        }

        var user = this.MapTo(new UserRegisterModel());
        user.Password = Password.ToSha256Str();
        var apiUrl = $"{setting.ApiUrl}/api/User/Register"; 
        try
        {
            var json =  JsonSerializer.Serialize(user); 
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resultStr = await httpService.Request(apiUrl,HttpMethod.Put,content);
            var result = JsonSerializer.Deserialize<ResponseResult<string>>(resultStr,
              Constants.DesSerializerOptions);
            if (result.Success)
            {
                MessageBox.Show($"注册成功，您的账号为:{result.Data}", "注册成功", MessageBoxButton.OK, MessageBoxImage.Information);
                View.Close();
                localStorage.Set(CachingKeys.RegisteredAccount,result.Data);
            }
            else
            {
                MessageBox.Show($"注册失败，原因:{result.Message}", "注册失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                await logger.LogAsync($"注册失败:{result.Message}", "用户注册");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"注册过程中程序出现异常:{e}", "用户注册");
            MessageBox.Show("注册过程中发生错误，请稍后重试", "注册失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
    }

}