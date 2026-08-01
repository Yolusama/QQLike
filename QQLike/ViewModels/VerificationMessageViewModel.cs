using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Domain;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Enum;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Views.Message;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class VerificationMessageViewModel(ISqlSugarClient sugarClient,
    IProjectLogger logger,
    IApiService apiService,
    ISessionStorage sessionStorage) : ViewModelBase<VerificationMessageView>
{
    [ObservableProperty]
    private bool? _isGroupVerify;

    [ObservableProperty]
    private string _notificationTitle = "好友通知";

    [ObservableProperty]
    private ObservableCollection<VerificationMessageItem> _notices;
    

    private async Task LoadNotices()
    {
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.GetAsync<List<VerificationMessageVO>>
               ($"api/{nameof(VerificationMessage)}/GetVerificationMessage/{user.UserId}",new {IsGroup= IsGroupVerify});
           if (res.Success)
           {
               Notices.Clear();
               foreach (var item in res.Data)
               {
                   var domainItem = item.MapTo(new VerificationMessageItem());
                   Notices.Add(domainItem);
               }
           }
           else
               MessageComponent.ShowMessage(Window.GetWindow(View), res.Message, MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Window.GetWindow(View), $"出现异常：{e.Message}", MessageType.Error);
            await logger.LogAsync($"加载验证信息出现异常:{e}", "验证消息");
        }
    }


    [RelayCommand]
    private void ToVerifyFriends()
    {
        IsGroupVerify = false;
    }

    [RelayCommand]
    private void ToVerifyGroups()
    {
        IsGroupVerify = true;
    }

    partial void OnIsGroupVerifyChanged(bool? value)
    {
        NotificationTitle = value!=null && value.Value ? "群聊通知" : "好友通知";
    }
    
}
