using System.Collections.ObjectModel;
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
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services.Interfaces;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class UserContactGroupViewModel(
    IApiService apiService,
    ISessionStorage sessionStorage,
    ISqlSugarClient sugarClient,
    IRedisCache redis,
    IProjectLogger logger,
    SysSetting setting) : ViewModelBase<UserContactGroupView>
{
    [ObservableProperty]
    private string _searchText = string.Empty;
    [ObservableProperty]
    private ObservableCollection<UserContactGroupItem> _userContactGroups = [];
    [ObservableProperty]
    private ObservableCollection<UserContactGroupViewItem> _searchResult = [];
    [ObservableProperty]
    private ObservableCollection<UserContactGroupViewItem> _selectedUserContacts = [];
    [ObservableProperty]
    private string _actionText;
    [ObservableProperty]
    private bool _isShareCommand;
    [ObservableProperty]
    private bool _isCreateGroupCommand;
    [ObservableProperty]
    private bool _isSearching;
    [ObservableProperty]
    private bool _isNotSearching;
    [ObservableProperty]
    private bool _isConfirmEnabled;
    [ObservableProperty]
    private ObservableCollection<ValueLabel<long>> _userChatGroups = [];
    [ObservableProperty] 
    private ValueLabel<long> _selectedGroup;
    

    private CancellationTokenSource? _searchCts;

    [RelayCommand]
    private async Task Load()
    {
        await LoadUserContactGroups();
        await LoadUserContactGroupSelections();
        Owner.Opacity = 0.25;
        if(IsCreateGroupCommand)
            ActionText = "创建群聊";
        else if(IsShareCommand)
            ActionText = "分享";
    }
    
    [RelayCommand]
    private void Close()
    {
        Owner.Opacity = 1;
        View.Close();
        Owner.Focus();
    }

    [RelayCommand]
    private void ChooseUserContact(UserContactGroupViewItem? item)
    {
        if(item is null || item.IsSelected) return;
        item.IsSelected = true;
        IsConfirmEnabled = true;
        SelectedUserContacts.Add(item);
    }

    [RelayCommand]
    private void RemoveSelectedUser(UserContactGroupViewItem? item)
    {
        if(item is null) return;
        SelectedUserContacts.Remove(item);
        item.IsSelected = false;
        IsConfirmEnabled = SelectedUserContacts.Count > 0;
    }

    [RelayCommand]
    private async Task SearchUsers()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        var search = SearchText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(search))
        {
            SearchResult.Clear();
            IsSearching = false;
            IsNotSearching = true;
            return;
        }

        IsSearching = true;
        IsNotSearching = false;
        try
        {
            await Task.Delay(250, token);

            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.GetAsync<List<UserContactGroupViewItem>>(
                $"api/{nameof(User)}/GetUserContactInfo/{user.UserId}", new { WithGroup = IsShareCommand, Search = search });

            token.ThrowIfCancellationRequested();

            if (res.Success)
            {
                SearchResult.Clear();
                res.Data.ForEach(item =>
                {
                    item.Source = $"来自 {item.Source}";
                    item.Avatar = $"{setting.ApiUrl}/Files/Images/{item.Avatar}";
                    item.ContactName = string.IsNullOrEmpty(item.Remark) ? item.Nickname : item.Remark;
                    SearchResult.Add(item);
                });
            }
            else
                MessageComponent.ShowMessage(Owner, $"搜索联系人失败,错误信息：{res.Message}", MessageType.Error);
        }
        catch (OperationCanceledException)
        {
            // Continuous input triggers cancellation frequently; no UI prompt is needed.
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"搜索联系人失败,出现异常：{e.Message}", MessageType.Error);
        }
    }
    
    private async Task LoadUserContactGroups()
    {
        IsNotSearching = true;
        IsSearching = false;
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.GetAsync<List<UserContactGroupVO>>(
                $"api/UserContact/ContactGroups/{user.UserId}",
                null);
            if (res.Success)
            {
                UserContactGroups.Clear();
                res.Data.ForEach(contactGroup =>
                {
                    var userContactGroupItem = new UserContactGroupItem
                    {
                        ContactGroupId = contactGroup.Id,
                        Name = contactGroup.Name,
                        UserContacts = new ObservableCollection<UserContactInfoItem>(),
                        UserContactCount = contactGroup.UserContactCount
                    };
                    UserContactGroups.Add(userContactGroupItem);
                });
                await redis.SetAsync($"{nameof(UserContactGroup)}_{user.UserId}", UserContactGroups,
                    TimeSpan.FromMinutes(3));
            }
            else
            {
                MessageComponent.ShowMessage(Owner, $"加载联系人分组失败,错误信息：{res.Message}",
                    MessageType.Error);
                await logger.LogAsync($"加载联系人分组失败,错误信息：{res.Message}", "用户联系人");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"加载联系人分组失败,出现异常：{e}", "用户联系人");
            MessageComponent.ShowMessage(Owner, $"加载联系人分组失败,出现异常：{e.Message}", MessageType.Error);
        }
    }
    
    private async Task LoadUserContactGroupSelections()
    {
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        try
        {
            var res = await apiService
                .GetAsync<List<ValueLabel<long>>>($"api/UserContact/GetUserContactGroupSelections/{user.UserId}", new {IsGroup = true});
            if(res.Success)
            {
                if (res.Data.Count == 0) return;
                UserChatGroups.Clear();
                SelectedGroup = res.Data.First();
                res.Data.ForEach(group => UserChatGroups.Add(group));
            }
            else
                MessageComponent.ShowMessage(View, $"获取分组失败：{res.Message}", MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(View, $"获取分组异常：{e.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    private async Task UserGroupExpand(UserContactGroupItem? item)
    {
        if (item == null) return;

        if (!item.IsExpanded)
        {
            item.ExpandIconAngle = 90;
            item.IsExpanded = true;
            await LoadUserContacts(item);
        }
        else
        {
            item.ExpandIconAngle = 0;
            item.IsExpanded = false;
        }
    }

    [RelayCommand]
    private async Task Confirm()
    {
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            if (IsCreateGroupCommand)
            {
                var dto = new CreateChatGroupDTO();
                dto.CreatorId = user.UserId;
                dto.GroupName = string.Join(',',SelectedUserContacts.Select(u => u.Nickname));
                dto.ChosenUserIds =SelectedUserContacts.Select(u => u.ContactId).ToList();
                dto.UserContactGroupId = SelectedGroup.Value;
                dto.GroupCreatorName = user.Nickname;
                var res = await apiService.PostAsync<string>(
                    $"api/{nameof(ChatGroup)}/CreateChatGroup", dto);
                if(res.Success)
                {
                    MessageComponent.ShowMessage(Owner, "创建群聊成功", MessageType.Success,3000L);
                    Close();
                    await redis.RemoveByPatternAsync($"{nameof(UserContactGroup)}_{dto.CreatorId}*");
                }
                else
                    MessageComponent.ShowMessage(Owner, $"创建群聊失败,错误信息：{res.Message}", MessageType.Error);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"确认操作失败,出现异常：{e.Message}", MessageType.Error);
        }
    }

    private async Task LoadUserContacts(UserContactGroupItem? item)
    {
        if (item == null) return;
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        var key = $"{nameof(UserContactGroup)}_{user.UserId}_{item.ContactGroupId}";
        if (await redis.ExistsAsync(key))
        {
            item.UserContacts.Clear();
            var cachedData = await redis.GetAsync<ObservableCollection<UserContactGroupViewItem>>(key);
            foreach (var data in cachedData)
            {
                ApplyContactDisplay(data);
                item.UserContacts.Add(data);
            }
            return;
        }
        var userContact = await sugarClient.Queryable<UserContact>()
            .Where(uc => uc.UserId == user.UserId && uc.UserContactGroupId == item.ContactGroupId)
            .Where(uc => uc.ContactStatus != UserContactStatus.删除.GetValue() &&
                         uc.ContactStatus != UserContactStatus.被删除.GetValue())
            .FirstAsync();

        if (userContact is null) return;

        item.UserContacts.Clear();
        if(!userContact.IsGroup)
        {
            var data = await sugarClient.Queryable<User>()
                .LeftJoin<UserContact>((u, uc) => u.Id == uc.ContactId && !uc.IsGroup)
                .LeftJoin<UserContactGroup>((u, uc, ucg) => uc.UserContactGroupId == ucg.Id && !ucg.IsGroup)
                .Where((u, uc, ucg) => uc.UserId == user.UserId && ucg.Id == item.ContactGroupId)
                .Where((u, uc, ucg) => uc.ContactStatus != UserContactStatus.删除.GetValue() &&
                                       uc.ContactStatus != UserContactStatus.被删除.GetValue())
                .Select((u, uc, ucg) => new UserContactGroupViewItem
                {
                    Nickname = u.Nickname,
                    Avatar = u.Avatar,
                    Account = u.Account,
                    Remark = uc.Remark,
                    ContactId = uc.ContactId,
                    Signature = u.Signature,
                    ContactName = u.Nickname,
                    UserContactGroupName = ucg.Name,
                    IsOnline = u.IsOnline.Value,
                })
                .ToListAsync();
            data.ForEach(e =>
            {
                ApplyContactDisplay(e);
                item.UserContacts.Add(e);
            });
        }
        if (IsShareCommand && userContact.IsGroup)
        {
            var groupContactData = await sugarClient.Queryable<ChatGroup>()
                .LeftJoin<UserContact>((c, uc) => c.Id == uc.ContactId && uc.IsGroup)
                .LeftJoin<UserContactGroup>((c, uc, ucg) => uc.UserContactGroupId == ucg.Id && ucg.IsGroup)
                .Where((c, uc, ucg) => uc.UserId == user.UserId && ucg.Id == item.ContactGroupId)
                .Where((c, uc, ucg) => uc.ContactStatus != UserContactStatus.删除.GetValue() &&
                                       uc.ContactStatus != UserContactStatus.被删除.GetValue())
                .Select((c, uc, ucg) => new UserContactGroupViewItem
                {
                    Avatar = c.Avatar,
                    Account = c.GroupNum,
                    Remark = uc.Remark,
                    ContactId = uc.ContactId,
                    GroupName = c.Name,
                    ContactName = c.Name,
                    UserContactGroupName = ucg.Name,
                })
                .ToListAsync();
            groupContactData.ForEach(e =>
            {
                ApplyContactDisplay(e);
                item.UserContacts.Add(e);
            });
        }
        await redis.SetAsync(key, item.UserContacts, TimeSpan.FromMinutes(3));
    }

    private void ApplyContactDisplay(UserContactGroupViewItem item)
    {
        var name = string.IsNullOrEmpty(item.Nickname) ? item.GroupName : item.Nickname;
        item.Avatar = $"{setting.ApiUrl}/Files/Images/{item.Avatar}";
        item.ContactName = string.IsNullOrEmpty(item.Remark) ? name : item.Remark;
        item.AccountText = $"({item.Account})";
        item.ContactToolTipText = $"{item.ContactName} {item.AccountText}";
    }
}