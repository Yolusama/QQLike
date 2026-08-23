using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Domain;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.DTO;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services;
using QQLike.Views.Message;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class VerificationMessageViewModel(ISqlSugarClient sugarClient,
    IProjectLogger logger,
    IApiService apiService,
    ISessionStorage sessionStorage,
    IRabbitMQProducer producer,
    SysSetting setting) : ViewModelBase<VerificationMessageView>
{
    [ObservableProperty]
    private bool _isGroupVerify;
    [ObservableProperty]
    private string _notificationTitle = "好友通知";
    [ObservableProperty]
    private ObservableCollection<VerificationMessageItem> _notices = [];
    [ObservableProperty]
    private bool _isChecked;

    /// <summary>
    /// 同意对话框相关
    /// </summary>
    [ObservableProperty]
    private bool _isAcceptDialogOpen;
    [ObservableProperty]
    private string _acceptRemark = string.Empty;
    [ObservableProperty]
    private long _selectedGroupId;
    [ObservableProperty]
    private ObservableCollection<ValueLabel<long>> _acceptableGroups = [];

    private VerificationMessageItem? _currentAcceptItem;

    [RelayCommand]
    private async Task LoadNotices()
    {
        var window = Window.GetWindow(View);
        var mainViewModel = window.GetViewModel<MainViewModel>();
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.GetAsync<List<VerificationMessageVO>>
               ($"api/{nameof(VerificationMessage)}/GetVerificationMessages/{user.UserId}",new {IsGroup = IsGroupVerify});
           if (res.Success)
           {
               var json = JsonSerializer.Serialize( new MQMessageBody
               {
                   Identifier = user.UserId,
                   Muted = false
               });
               await producer.Produce(nameof(VerificationMessage),Constants.MQExchange,$"{nameof(VerificationMessage)}_{user.UserId}", json);
               Notices.Clear();
               foreach (var item in res.Data)
               {
                   var domainItem = item.MapTo(new VerificationMessageItem());
                   domainItem.Avatar = $"{setting.ApiUrl}/Files/Images/{item.Avatar}";
                   Notices.Add(domainItem);
               }
           }
           else
               MessageComponent.ShowMessage(window, res.Message, MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(window, $"出现异常：{e.Message}", MessageType.Error);
            await logger.LogAsync($"加载验证信息出现异常:{e}", "验证消息");
        }
    }

    [RelayCommand]
    private async Task AcceptVerify(VerificationMessageItem item)
    {
        item.IsPopupOpen = false;
        _currentAcceptItem = item;
        await LoadAcceptableGroups(item.IsGroup);
        SelectedGroupId = item.UserContactGroupId;
        AcceptRemark = item.Remark ?? string.Empty;
        IsAcceptDialogOpen = true;
    }

    private async Task LoadAcceptableGroups(bool isGroup)
    {
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.GetAsync<List<ValueLabel<long>>>(
                $"api/UserContact/GetUserContactGroupSelections/{user.UserId}", new { IsGroup = isGroup });
            if (res.Success)
            {
                AcceptableGroups.Clear();
                res.Data.ForEach(AcceptableGroups.Add);
            }
            else
                MessageComponent.ShowMessage(Window.GetWindow(View), $"加载分组失败：{res.Message}", MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"加载分组异常:{e}", "验证消息");
            MessageComponent.ShowMessage(Window.GetWindow(View), $"加载分组异常：{e.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    private async Task ConfirmAcceptVerify()
    {
        if (_currentAcceptItem == null) return;
        IsAcceptDialogOpen = false;

        using var worker = sugarClient.CreateContext();
        try
        {
            var item = _currentAcceptItem;
            await worker.Db.Updateable<VerificationMessage>()
                .SetColumns(e => e.Status == VerificationMessageStatus.已同意.GetValue())
                .Where(e => e.UserId == item.UserId && e.ContactId == item.ContactId)
                .ExecuteCommandAsync();
            await worker.Db.Updateable<VerificationMessage>()
                .SetColumns(e => e.Status == VerificationMessageStatus.已通过.GetValue())
                .Where(e => e.UserId == item.ContactId && e.ContactId == item.UserId)
                .ExecuteCommandAsync();
            item.Status = VerificationMessageStatus.已同意.GetValue();

            var userContact = new UserContact
            {
                UserId = item.UserId,
                ContactId = item.ContactId,
                IsGroup = item.IsGroup,
                UserContactGroupId = SelectedGroupId > 0 ? SelectedGroupId : item.UserContactGroupId,
                ContactStatus = UserContactStatus.正常.GetValue(),
                DeleteMark = 0,
                CreateTime = DateTime.Now,
                Remark = AcceptRemark
            };
            var oppositeContact = userContact.MapTo<UserContact, UserContact>();
            oppositeContact.Remark = string.Empty;
            oppositeContact.UserId = userContact.ContactId;
            oppositeContact.ContactId = userContact.UserId;
            await worker.Db.Insertable(new List<UserContact> { userContact, oppositeContact })
                .ExecuteCommandAsync();
            worker.Commit();

            var msgText = item.IsGroup ? "已加入群聊" : "已通过对方好友请求";
            IsChecked = false;
            MessageComponent.ShowMessage(Window.GetWindow(View), msgText, MessageType.Success);
        }
        catch (Exception e)
        {
            await logger.LogAsync($"同意验证出现异常:{e}", "验证消息");
            MessageComponent.ShowMessage(Window.GetWindow(View), $"出现异常：{e.Message}", MessageType.Error);
        }
        finally
        {
            _currentAcceptItem = null;
        }
    }

    [RelayCommand]
    private void CancelAcceptDialog()
    {
        IsAcceptDialogOpen = false;
        _currentAcceptItem = null;
    }

    [RelayCommand]
    private async Task RejectVerify(VerificationMessageItem item)
    {
        item.IsPopupOpen = false;
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
            IsChecked = false;
            worker.Commit();
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
        item.IsPopupOpen = false;
        using var worker = sugarClient.CreateContext();
        try
        {
            await worker.Db.Updateable<VerificationMessage>()
                .SetColumns(e=>e.Status==VerificationMessageStatus.忽略.GetValue())
                .Where(e=>e.UserId==item.UserId && e.ContactId == item.ContactId)
                .ExecuteCommandAsync();
            item.Status = VerificationMessageStatus.忽略.GetValue();
            worker.Commit();
            IsChecked = false;
            MessageComponent.ShowMessage(Window.GetWindow(View), "已忽略该验证请求");
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
