using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Domain;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Enum;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
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
    private bool _isGroupView = false;
    
    
    [RelayCommand]
    private void SwitchUserView()
    {
        IsGroupView = false;
    }

    [RelayCommand]
    private void SwitchGroupView()
    {
        IsGroupView = true;
    }

    [RelayCommand]
    private async Task LoadUserContactGroups()
    {
        try
        {
           var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
           var res = await apiService.GetAsync<List<long>>($"api/UserContact/ContactGroups/{user.UserId}",
               null);
           if (res.Success)
           {
               UserContactGroups.Clear();
               res.Data.ForEach(contactGroupId =>
               {
                   var userContactGroupItem = new UserContactGroupItem()
                   {
                       ContactGroupId = contactGroupId,
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
            // 展开：旋转图标 + 加载联系人
            item.ExpandIconAngle = 90;
            item.IsExpanded = true;

            var userContactInfos = await sugarClient.Queryable<UserContact>()
                .InnerJoin<User>((uc, u) => uc.ContactId == u.Id)
                .Where(uc => uc.UserContactGroupId == item.ContactGroupId &&
                             uc.IsGroup == IsGroupView)
                .Select((uc, u) => new { uc, u })
                .ToListAsync(e => new UserContactInfo
                {
                    Avatar = e.u.Avatar,
                    ContactId = e.uc.ContactId,
                    IsOnline = e.u.IsOnline.Value,
                    Nickname = e.u.Nickname,
                    Account = e.u.Account,
                    Signature = e.u.Signature,
                });
            userContactInfos.ForEach(userContactInfo =>
            {
                item.UserContacts.Add(userContactInfo);
            });
        }
        else
        {
            // 收起：旋转图标回原位
            item.ExpandIconAngle = 0;
            item.IsExpanded = false;
            item.UserContacts.Clear();
        }
    }

    [RelayCommand]
    private void OpenUserManage()
    {
         windowFactory.GetAndShowWindow<UserContactManageView>();
    }
}