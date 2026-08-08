using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Domain;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Views.User;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class UserContactManageViewModel(
    ISqlSugarClient sugarClient,
    SysSetting setting,
    IProjectLogger logger,
    IApiService apiService,
    ISessionStorage sessionStorage,
    MessageComponent messageComponent)
    : ViewModelBase<UserContactManageView>
{
    [ObservableProperty] private string _searchText;
    [ObservableProperty] private string _newGroupName;
    [ObservableProperty] private bool _isGroupDialogOpen;
    [ObservableProperty] private bool _isGroup;
    [ObservableProperty] private ObservableCollection<UserContactGroupingItem> _groupingData = [];
    [ObservableProperty] private ObservableCollection<UserContactManageItem> _friends = [];

    private long _currentContactGroupId;

    partial void OnSearchTextChanged(string value)
    {

    }

    [RelayCommand]
    private void AddGroup()
    {
        IsGroupDialogOpen = true;
    }

    [RelayCommand]
    private async Task GetGrouping()
    {
        GroupingData.Clear();
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.GetAsync<List<UserContactGroupingVO>>
                ($"api/UserContact/GetUserContactGrouping/{user.UserId}", null);
            if (res.Success)
            {
                foreach (var group in res.Data)
                {
                    GroupingData.Add(new UserContactGroupingItem
                    {
                        UserContactGroupId = group.ContactGroupId,
                        Name = group.GroupName,
                        ContactCount = group.ContactCount,
                    });
                }
            }
            else
                MessageComponent.ShowMessage(View, $"获取分组失败：{res.Message}", MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"获取分组异常：{e}", "好友管理器");
            MessageComponent.ShowMessage(View, $"出现异常：{e.Message}", MessageType.Error);
            throw;
        }
    }

    [RelayCommand]
    private async Task ConfirmAddGroup()
    {
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.PostAsync<long>("api/UserContact/AddUserContactGroup", new UserContactGroupModel
            {
                UserId = user.UserId,
                Name = NewGroupName,
                IsGroup = false
            });
            if (res.Success)
            {
                MessageComponent.ShowMessage(View, $"添加分组成功", MessageType.Success);
                CancelAddGroup();
                await GetGrouping();
            }
            else
                MessageComponent.ShowMessage(View, $"添加分组失败：{res.Message}", MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"添加分组异常：{e}", "好友管理器");
            MessageComponent.ShowMessage(View, $"出现异常：{e.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    private void CancelAddGroup()
    {
        NewGroupName = string.Empty;
        IsGroupDialogOpen = false;
    }

    [RelayCommand]
    private async Task LoadFriends()
    {
        Friends.Clear();
        try
        {
            var res = await apiService.GetAsync<List<UserContactManageVO>>
                ($"api/UserContact/GetUserManageFriends/{sessionStorage.Get<UserLoginVO>(CachingKeys.User).UserId}/{_currentContactGroupId}", null);
            if (res.Success)
            {
                foreach (var item in res.Data)
                {
                    item.Avatar = $"{setting.ApiUrl}/Files/Images/{item.Avatar}";
                    Friends.Add(item.MapTo<UserContactManageVO, UserContactManageItem>());
                }
            }
            else
                messageComponent.Show(View, $"获取好友失败：{res.Message}", MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            messageComponent.Show(View, $"获取好友异常：{e.Message}", MessageType.Error);
            throw;
        }
    }

    [RelayCommand]
    private async Task SelectGroup(UserContactGroupingItem? item)
    {
        if (item == null) return;
        _currentContactGroupId = item.UserContactGroupId;
        await LoadFriends();
    }
    
    [RelayCommand]
    private async Task LoadData()
    {
        await GetGrouping();
        await LoadFriends();
    }
}