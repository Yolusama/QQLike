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
using QQLike.Services.Interfaces;
using QQLike.Views.User;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class UserProfileViewModel(SysSetting setting, 
    ISessionStorage sessionStorage, 
    IWindowFactory windowFactory,
    IApiService apiService,
    ISqlSugarClient sugarClient) 
    : ViewModelBase<UserProfileView>
{
    [ObservableProperty]
    private string _nickname;
    [ObservableProperty]
    private string _avatar;
    [ObservableProperty]
    private string _account;
    [ObservableProperty]
    private string _remark;
    [ObservableProperty]
    private ObservableCollection<ValueLabel<long>> _friendGroups = [];
    [ObservableProperty]
    private long _selectedGroupId;
    [ObservableProperty]
    private string _genderText;
    [ObservableProperty]
    private string _signature;
    
    public bool UserProfileEditable {get; private set;}

    [RelayCommand]
    private async Task LoadData()
    {
        await LoadUserInfo();
        await LoadFriendGroups();
    }

    private async Task LoadUserInfo()
    {
        if (!View.IsVisible)
        {
            if(sessionStorage.KeyExists(CachingKeys.UserContactCurrentUserId))
                sessionStorage.Remove(CachingKeys.UserContactCurrentUserId);
            return;
        }
        var toSeeUserId = sessionStorage.Get<string>(CachingKeys.UserContactCurrentUserId);
        if(string.IsNullOrEmpty(toSeeUserId))
            return;

        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        var userInfo = await sugarClient.Queryable<User>()
            .InnerJoin<UserContact>((u, uc) => u.Id == uc.ContactId)
            .InnerJoin<UserContactGroup>((u,uc, ucg) => uc.UserContactGroupId == ucg.Id)
            .Where((u, uc,ucg) => uc.UserId == user.UserId && uc.ContactId == toSeeUserId)
            .Select((u, uc, ucg) => new UserProfileVO
            {
                Nickname = u.Nickname,
                Avatar = u.Avatar,
                Account = u.Account,
                Remark = uc.Remark,
                Gender = u.Gender,
                Signature = u.Signature,
                UserContactGroupId = uc.UserContactGroupId,
                Birthday = u.Birthday.Value
            })
            .FirstAsync();

        if (userInfo == null)
        {
            Nickname = string.Empty;
            Avatar = string.Empty;
            Account = string.Empty;
            Remark = string.Empty;
            Signature = string.Empty;
            return;
        }

        Nickname = userInfo.Nickname;
        Avatar  = $"{setting.ApiUrl}/Files/Images/{userInfo.Avatar}";
        Account = userInfo.Account;
        Remark  = userInfo.Remark;
        Signature = userInfo.Signature;
        SelectedGroupId = userInfo.UserContactGroupId;
        GenderText =  userInfo.Gender == UserGender.男.GetValue() ? "男" : "女";
        UserProfileEditable = user.UserId == toSeeUserId;
    }
    
    private async Task LoadFriendGroups()
    {
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.GetAsync<List<ValueLabel<long>>>
                ($"api/userContact/GetUserContactGroupSelections/{user.UserId}",new {IsGroup = false});
            if (res.Success)
            {
                FriendGroups.Clear();
                foreach (var group in res.Data)
                    FriendGroups.Add(group);
            }
            else
                MessageComponent.ShowMessage(Window.GetWindow(View), res.Message, MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Window.GetWindow(View), $"程序程序异常：{e.Message}", MessageType.Error);
        }
    }
}