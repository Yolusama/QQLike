using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Entity.Common;
using QQLike.Entity.Enum;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Views.User;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class UserContactManageViewModel(ISqlSugarClient sugarClient,
    IProjectLogger logger,
    IApiService apiService,
    ISessionStorage sessionStorage) 
    : ViewModelBase<UserContactManageView>
{
    [ObservableProperty] 
    private string _searchText;
    [ObservableProperty]
    private string _newGroupName;
    [ObservableProperty]
    private bool _isGroupDialogOpen;
    [ObservableProperty]
    private ObservableCollection<object> _groups;
    [ObservableProperty]
    private ObservableCollection<object> _friends;

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
        Groups.Clear();
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.GetAsync<List<UserContactGroupingVO>>
               ($"api/UserContact/GetUserContactGrouping/{user.UserId}", null);
            if (res.Success)
            {
                foreach (var group in res.Data)
                    Groups.Add(group);
            }
            else
                MessageComponent.ShowMessage(View,$"获取分组失败：{res.Message}",MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"获取分组异常：{e}", "好友管理器");
            MessageComponent.ShowMessage(View,$"出现异常：{e.Message}",MessageType.Error);
            throw;
        }
    }

    [RelayCommand]
    private async Task ConfirmAddGroup()
    {
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.PostAsync<string>($"api/UserContact/AddUserContactGrouping", new
            {
                user.UserId,
                GroupName = NewGroupName,
                IsGroup = false
            });
            if (res.Success)
            {
                MessageComponent.ShowMessage(View,$"添加分组成功",MessageType.Success);
                CancelAddGroup();
                await GetGrouping();
            }
            else
                MessageComponent.ShowMessage(View,$"添加分组失败：{res.Message}",MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"添加分组异常：{e}", "好友管理器");
            MessageComponent.ShowMessage(View,$"出现异常：{e.Message}",MessageType.Error);
        }
    }

    [RelayCommand]
    private void CancelAddGroup()
    {
        NewGroupName = string.Empty;
        IsGroupDialogOpen = false;
    }
}