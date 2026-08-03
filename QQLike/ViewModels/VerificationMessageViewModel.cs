using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Domain;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.Enum;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Views.Message;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class VerificationMessageViewModel(ISqlSugarClient sugarClient,
    IProjectLogger logger,
    IApiService apiService,
    ISessionStorage sessionStorage,
    SysSetting setting) : ViewModelBase<VerificationMessageView>
{
    [ObservableProperty]
    private bool _isGroupVerify;

    [ObservableProperty]
    private string _notificationTitle = "好友通知";

    [ObservableProperty]
    private ObservableCollection<VerificationMessageItem> _notices = [];
    

    [RelayCommand]
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
                   domainItem.Avatar = $"{setting.ApiUrl}/Files/Images/{item.Avatar}";
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
    private async Task AcceptVerify(VerificationMessageItem item)
    {
        using var worker = sugarClient.CreateContext();
        try
        {
            await worker.Db.Updateable<VerificationMessage>()
                .SetColumns(e=>e.Status==VerificationMessageStatus.已同意.GetValue())
                .Where(e=>e.UserId==item.UserId && e.ContactId == item.ContactId)
                .ExecuteCommandAsync();
            await worker.Db.Updateable<VerificationMessage>()
                .SetColumns(e=>e.Status==VerificationMessageStatus.已通过.GetValue())
                .Where(e=>e.UserId==item.ContactId && e.ContactId==item.UserId)
                .ExecuteCommandAsync();
            item.Status = VerificationMessageStatus.已同意.GetValue();
            worker.Commit();
            var msgText = item.IsGroup ? "已加入群聊" : "已通过对方好友请求";
            MessageComponent.ShowMessage(Window.GetWindow(View), msgText, MessageType.Success);
        }
        catch (Exception e)
        {
            await logger.LogAsync($"同意验证出现异常:{e}", "验证消息");
            MessageComponent.ShowMessage(Window.GetWindow(View), $"出现异常：{e.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    private async Task RejectVerify(VerificationMessageItem item)
    {
        using var worker =  sugarClient.CreateContext();
        try
        {
            await worker.Db.Updateable<VerificationMessage>()
                .SetColumns(e=>e.Status==VerificationMessageStatus.已拒绝.GetValue())
                .Where(e=>e.UserId==item.UserId && e.ContactId == item.ContactId)
                .ExecuteCommandAsync();
            await worker.Db.Updateable<VerificationMessage>()
                .SetColumns(e=>e.Status==VerificationMessageStatus.被拒绝.GetValue())
                .Where(e=>e.UserId==item.ContactId && e.ContactId==item.UserId)
                .ExecuteCommandAsync();
            item.Status = VerificationMessageStatus.已拒绝.GetValue();
            MessageComponent.ShowMessage(Window.GetWindow(View), "已拒绝验证请求", MessageType.Error);
        }
        catch (Exception e)
        {
            await logger.LogAsync($"拒绝验证出现异常:{e}", "验证消息");
            MessageComponent.ShowMessage(Window.GetWindow(View), $"出现异常：{e.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    private async Task IgnoreVerify(VerificationMessageItem item)
    {
        using var worker = sugarClient.CreateContext(); 
        try
        {
            await worker.Db.Updateable<VerificationMessage>()
                .SetColumns(e=>e.Status==VerificationMessageStatus.忽略.GetValue())
                .Where(e=>e.UserId==item.UserId && e.ContactId == item.ContactId)
                .ExecuteCommandAsync();
            item.Status = VerificationMessageStatus.忽略.GetValue();
            MessageComponent.ShowMessage(Window.GetWindow(View), "已忽略该验证请求", MessageType.Error);
        }
        catch (Exception e)
        {
            await logger.LogAsync($"忽略验证出现异常:{e}", "验证消息");
            MessageComponent.ShowMessage(Window.GetWindow(View), $"出现异常：{e.Message}", MessageType.Error);
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

    partial void OnIsGroupVerifyChanged(bool value)
    {
        NotificationTitle = value ? "群聊通知" : "好友通知";
    }
}
