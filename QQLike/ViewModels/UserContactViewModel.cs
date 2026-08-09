using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Domain;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Enum;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services.Interfaces;
using QQLike.Views.User;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class UserContactViewModel(ISqlSugarClient sugarClient,
    IProjectLogger logger,
    IApiService apiService,
    IWindowFactory windowFactory,
    ISessionStorage sessionStorage) : ViewModelBase<UserContactView>
{
    [ObservableProperty]
    private ObservableCollection<UserContactGroupItem> _userContactGroups = [];
    [ObservableProperty]
    private bool _isGroupView;
    [ObservableProperty]
    private bool _isUserView = true;
    [ObservableProperty]
    private Brush _userViewActivatedBackColor = Brushes.White;
    [ObservableProperty]
    private Brush _groupViewActivatedBackColor = Brushes.Transparent;
    [ObservableProperty] 
    private bool _isUserProfileVisible;
    [ObservableProperty] 
    private bool _isGroupProfileVisible;
    
    [RelayCommand]
    private async Task SwitchUserView()
    {
        ApplyViewState(false);
        await RefreshLoadedGroupsAsync();
    }

    [RelayCommand]
    private async Task SwitchGroupView()
    {
        ApplyViewState(true);
        await RefreshLoadedGroupsAsync();
    }

    private void ApplyViewState(bool isGroupView)
    {
        IsGroupView = isGroupView;
        IsUserView = !isGroupView;
        UserViewActivatedBackColor = isGroupView ? Brushes.Transparent : Brushes.White;
        GroupViewActivatedBackColor = isGroupView ? Brushes.White : Brushes.Transparent;
    }

    private async Task RefreshLoadedGroupsAsync()
    {
        await LoadUserContactGroups();
    }
    

    [RelayCommand]
    private async Task LoadUserContactGroups()
    {
        try
        {
           var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
           var res = await apiService.GetAsync<List<UserContactGroupVO>>($"api/UserContact/ContactGroups/{user.UserId}",
               new {IsGroup = IsGroupView});
           if (res.Success)
           {
               UserContactGroups.Clear();
               res.Data.ForEach(contactGroup =>
               {
                   var userContactGroupItem = new UserContactGroupItem()
                   {
                       ContactGroupId = contactGroup.Id,
                       Name = contactGroup.Name,
                       UserContacts = new ObservableCollection<UserContactInfo>()
                   };
                   UserContactGroups.Add(userContactGroupItem);
               });
           }
           else
           {
               MessageComponent.ShowMessage(Window.GetWindow(View), $"加载联系人分组失败,错误信息：{res.Message}", 
                   MessageType.Error);
               await logger.LogAsync($"加载联系人分组失败,错误信息：{res.Message}","用户联系人");
           }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"加载联系人分组失败,出现异常：{e}","用户联系人");
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
            item.UserContacts.Clear();
        }
    }

    private async Task LoadUserContacts(UserContactGroupItem? item)
    {
        if(item == null) return;
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        if(IsUserView)
        {
            var data = await sugarClient.Queryable<User>()
                .LeftJoin<UserContact>((u, uc) => u.Id == uc.UserId && !uc.IsGroup)
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
            data.ForEach(item.UserContacts.Add);
        }

        if (IsGroupView)
        {
            var data = await sugarClient.Queryable<ChatGroup>()
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
            data.ForEach(item.UserContacts.Add);
        }
        
    }

    [RelayCommand]
    private void OpenUserContactManage()
    {
         windowFactory.GetAndShowWindow<UserContactManageView>();
    }
}