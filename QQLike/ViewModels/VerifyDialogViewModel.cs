using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;

namespace QQLike.ViewModels;

public partial class VerifyDialogViewModel(IProjectLogger logger
,IApiService apiService,
ISessionStorage sessionStorage,
SysSetting setting) : ViewModelBase<VerifyDialog>
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _nickname;
    [ObservableProperty] private string _account;
    [ObservableProperty] private string _avatar;
    [ObservableProperty] private string _verifyMessage;
    [ObservableProperty] private string _remark;
    [ObservableProperty] private long _selectedGroupId;
    [ObservableProperty] private ObservableCollection<ValueLabel<long>> _groups = [];
    [ObservableProperty] private string _groupNum;
    private string _contactId;
    public bool IsGroup { get; set; }
    public string Source { get; set; }
    
    public Func<Task>? ConfirmCallback { get; set; }
    public Func<Task>? CancelCallback { get; set; }

    [RelayCommand]
    private async Task LoadUserVerifyInfo()
    {
        Groups.Clear();
        try
        {
            var res = await apiService.GetAsync<UserVerifyInfo>(
                "api/User/GetUserVerifyInfo",new { Account });
            if (res.Success)
            {
                var userInfo = res.Data;
                Nickname = userInfo.Nickname;
                Avatar = $"{setting.ApiUrl}/Files/Images/{userInfo.Avatar}";
                _contactId =  userInfo.UserId;
                await LoadUserContactGroups();
            }
            else 
                NotificationComponent.ShowNotification(View,res.Message,MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            NotificationComponent.ShowNotification(View,$"程序出现异常：{e.Message}",MessageType.Error);
        }
    }

    [RelayCommand]
    private async Task Send()
    {
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var model = new VerificationMessageModel();
            model.IsGroup = IsGroup;
            model.ContactId = _contactId;
            model.NeedConfirm = true;
            model.Source = Source;
            model.VerificationMessage = VerifyMessage;
            model.Status = VerificationMessageStatus.验证中.GetValue();
            model.UserId = user.UserId;
            model.UserContactGroupId = SelectedGroupId;
            model.Remark = Remark;
            
            var res = await apiService.PostAsync<object>($"api/{nameof(VerificationMessage)}/AddVerificationMessage",
                model);
            if (res.Success)
            {
                NotificationComponent.ShowNotification(View,"已发送验证信息",MessageType.Success);
                if(ConfirmCallback!=null)
                    await ConfirmCallback.Invoke();
            }
            else
                NotificationComponent.ShowNotification(View,res.Message,MessageType.Error);
            View.Close();
        }
        catch (Exception ex)
        {
            await logger.LogAsync($"发送验证信息出现异常:{ex}", "验证消息");
            NotificationComponent.ShowNotification(View,$"发送验证信息出现异常：{ex.Message}",MessageType.Error);
        }
       
    }

    [RelayCommand]
    private async Task Cancel()
    {
        if(CancelCallback!=null)
           await CancelCallback.Invoke();
        View.Close();
    }

    [RelayCommand]
    private void Close()
    {
        View.Close();
    }

    private async Task LoadUserContactGroups()
    {
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.GetAsync<List<ValueLabel<long>>>
                ($"api/UserContact/GetUserContactGroupSelections/{user.UserId}", new { IsGroup });
            if (res.Success)
            {
               Groups.Clear();
               res.Data.ForEach(Groups.Add);
            }
            else
                NotificationComponent.ShowNotification(View, res.Message, MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            NotificationComponent.ShowNotification(View,$"程序出现异常：{e.Message}",MessageType.Error);
        }
    }
}