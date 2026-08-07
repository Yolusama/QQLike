using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Domain;
using QQLike.Entity;
using QQLike.Entity.Configuration;
using QQLike.Functional.Instructure;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class ComprehensiveSearchViewModel(
    ISqlSugarClient sugarClient,
    IApiService apiService,
    ISessionStorage sessionStorage,
    SysSetting setting) : ViewModelBase<ComprehensiveSearch>
{
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private string _activeTab = "Group";

    [ObservableProperty] private bool _isGroupTabActive = true;

    [ObservableProperty] private bool _isUserTabActive = false;

    [ObservableProperty] private bool _isLoading;

    private int page = 1;
    private int pageSize = 10;
    private bool _hasMoreGroupData = true;
    private bool _hasMoreUserData = true;

    partial void OnActiveTabChanged(string value)
    {
        IsGroupTabActive = value == "Group";
        IsUserTabActive = value == "User";
    }

    [ObservableProperty] private ObservableCollection<UserComprehensiveItem> _userResults = [];

    [ObservableProperty] private ObservableCollection<GroupComprehensiveItem> _groupResults = [];

    [RelayCommand]
    private async Task SwitchTab(string tab)
    {
        ActiveTab = tab;
        await LoadData();
    }

    [RelayCommand]
    private async Task LoadData()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        try
        {
            var keyword = SearchText.Trim();
            if (IsGroupTabActive)
            {
                if (!_hasMoreGroupData)
                    return;

                var groups = await sugarClient.Queryable<ChatGroup>()
                    .Where(g => g.DeleteMark == 0 &&
                                (g.Name.Contains(keyword) || g.GroupNum.Contains(keyword) ||
                                 g.Description.Contains(keyword)))
                    .OrderBy(g => g.CurrentCount, OrderByType.Desc)
                    .Select(g => new GroupComprehensiveItem
                    {
                        Name = g.Name,
                        Avatar = g.Avatar,
                        GroupNum = g.GroupNum,
                        Description = g.Description,
                        CurrentCount = g.CurrentCount,
                        TotalCount = g.MaxCount,
                        CreateTime = g.CreateTime
                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (groups.Count == 0)
                {
                    _hasMoreGroupData = false;
                    return;
                }

                foreach (var group in groups)
                {
                    group.Avatar = $"{setting.ApiUrl}/Files/Images/{group.Avatar}";
                    GroupResults.Add(group);
                }

                _hasMoreGroupData = groups.Count >= pageSize;
                page++;
            }

            if (IsUserTabActive)
            {
                if (!_hasMoreUserData)
                    return;

                var users = await sugarClient.Queryable<User>()
                    .Where(u => u.Nickname.Contains(keyword) || u.Account.Contains(keyword))
                    .OrderBy(u => u.LastLoginTime, OrderByType.Desc)
                    .Select(u => new UserComprehensiveItem
                    {
                        Nickname = u.Nickname,
                        Avatar = u.Avatar,
                        Account = u.Account
                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (users.Count == 0)
                {
                    _hasMoreUserData = false;
                    return;
                }

                foreach (var user in users)
                {
                    user.Avatar = $"{setting.ApiUrl}/Files/Images/{user.Avatar}";
                    UserResults.Add(user);
                }

                _hasMoreUserData = users.Count >= pageSize;
                page++;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        page = 1;
        _hasMoreGroupData = true;
        _hasMoreUserData = true;
        UserResults.Clear();
        GroupResults.Clear();

        await LoadData();
    }

    [RelayCommand]
    private void JoinGroup(GroupComprehensiveItem? item)
    {
        if (item is null)
            return;
        const string source = "来自 群聊搜索";
        VerifyDialog.ShowVerifyDialog(Window.GetWindow(View), source,true);
    }

    [RelayCommand]
    private void AddUser(UserComprehensiveItem? item)
    {
        if (item is null)
            return;
        const string source = "来自 好友搜索";
        VerifyDialog.ShowVerifyDialog(Window.GetWindow(View), source);
    }
}