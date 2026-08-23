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
    private string _searchText;
    [ObservableProperty]
    private ObservableCollection<UserContactGroupItem> _userContactGroups;
    [ObservableProperty]
    private string _actionText;
    [ObservableProperty]
    private bool _isShareCommand;
    [ObservableProperty]
    private bool _isCreateGroupCommand;
    

    [RelayCommand]
    private async Task LoadUserContactGroups()
    {
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

    private async Task LoadUserContacts(UserContactGroupItem? item)
    {
        if (item == null) return;
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        var key = $"{nameof(UserContactGroup)}_{user.UserId}_{item.ContactGroupId}";
        if (await redis.ExistsAsync(key))
        {
            var cachedData = await redis.GetAsync<ObservableCollection<UserContactInfoItem>>(key);
            item.UserContacts = cachedData;
            return;
        }
        var userContact = await sugarClient.Queryable<UserContact>()
            .Where(uc => uc.UserId == user.UserId && uc.UserContactGroupId == item.ContactGroupId)
            .Where(uc => uc.ContactStatus != UserContactStatus.删除.GetValue() &&
                         uc.ContactStatus != UserContactStatus.被删除.GetValue())
            .FirstAsync();
        
        item.UserContacts.Clear();
        if(!userContact.IsGroup)
        {
            var data = await sugarClient.Queryable<User>()
                .LeftJoin<UserContact>((u, uc) => u.Id == uc.ContactId && !uc.IsGroup)
                .LeftJoin<UserContactGroup>((u, uc, ucg) => uc.UserContactGroupId == ucg.Id && !ucg.IsGroup)
                .Where((u, uc, ucg) => uc.UserId == user.UserId && ucg.Id == item.ContactGroupId)
                .Where((u, uc, ucg) => uc.ContactStatus != UserContactStatus.删除.GetValue() &&
                                       uc.ContactStatus != UserContactStatus.被删除.GetValue())
                .Select((u, uc, ucg) => new UserContactInfoItem
                {
                    Nickname = u.Nickname,
                    Avatar = u.Avatar,
                    Account = u.Account,
                    Remark = uc.Remark,
                    ContactId = uc.ContactId,
                    Signature = u.Signature,
                    UserContactGroupName = ucg.Name,
                    IsOnline = u.IsOnline.Value,
                })
                .ToListAsync();
            data.ForEach(e =>
            {
                e.Avatar = $"{setting.ApiUrl}/Files/Images/{e.Avatar}";
                item.UserContacts.Add(e);
            });
        }
        if (IsShareCommand && userContact.IsGroup)
        {
            var groupContactData = await sugarClient.Queryable<ChatGroup>()
                .LeftJoin<UserContact>((c, uc) => c.Id == uc.UserId && uc.IsGroup)
                .LeftJoin<UserContactGroup>((c, uc, ucg) => uc.UserContactGroupId == ucg.Id && ucg.IsGroup)
                .Where((c, uc, ucg) => uc.UserId == user.UserId && ucg.Id == item.ContactGroupId)
                .Where((c, uc, ucg) => uc.ContactStatus != UserContactStatus.删除.GetValue() &&
                                       uc.ContactStatus != UserContactStatus.被删除.GetValue())
                .Select((c, uc, ucg) => new UserContactInfoItem
                {
                    Avatar = c.Avatar,
                    Account = c.GroupNum,
                    Remark = uc.Remark,
                    ContactId = uc.ContactId,
                    GroupName = c.Name,
                    UserContactGroupName = ucg.Name,
                })
                .ToListAsync();
            groupContactData.ForEach(e =>
            {
                e.Avatar = $"{setting.ApiUrl}/Files/Images/{e.Avatar}";
                item.UserContacts.Add(e);
            });
        }
        await redis.SetAsync(key, item.UserContacts, TimeSpan.FromMinutes(3));
    }
}