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
using QQLike.Services.Interfaces;

namespace QQLike.ViewModels;

public partial class UserSearchHeaderViewModel(ISqlSugarClient sugarClient, SysSetting setting, IWindowFactory windowFactory) : ViewModelBase<UserSearchHeader>
{
    [ObservableProperty]
    private string _searchText = string.Empty;
    [ObservableProperty]
    private bool _isSearching = false;
    [ObservableProperty]
    private bool _isAdding = false;
    [ObservableProperty]
    private ObservableCollection<UserContactItem> _userResults = new();
    [ObservableProperty]
    private ObservableCollection<UserContactItem> _groupResults = new ();

    private CancellationTokenSource? _cts;

    public void ShowSearch()
    {
        IsSearching = true;
    }
    
    public void CancelSearch()
    {
        _cts?.Cancel();
        IsSearching = false;
        SearchText = string.Empty;
    }

    [RelayCommand]
    private async Task Search()
    {
        // 取消上一次搜索
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        var searchText = SearchText.Trim();
        if (string.IsNullOrEmpty(searchText))
        {
            IsSearching = false;
            UserResults.Clear();
            GroupResults.Clear();
            return;
        }

        try
        {
            // 300ms 防抖，等待用户停止输入
            await Task.Delay(300, token);
            
            UserResults.Clear();
            GroupResults.Clear();

            var userResults = await sugarClient.Queryable<User>()
                .InnerJoin<UserContact>((u,uc)=>u.Id == uc.ContactId && !uc.IsGroup && uc.DeleteMark == 0)
                .InnerJoin<UserContactGroup>((u, uc, ucg) => uc.UserContactGroupId == ucg.Id && !ucg.IsGroup)
                .Select((u, uc, ucg) => new UserContactItem
                {
                    ContactIdentifier = u.Account,
                    ContactName = u.Nickname,
                    Avatar = u.Avatar,
                    GroupFrom = ucg.Name
                })
                .Where(e=>e.ContactName.Contains(searchText)
                          || e.GroupFrom.Contains(searchText) || e.ContactIdentifier.Contains(searchText))
                .ToListAsync(token);

            // 再次检查是否已取消
            token.ThrowIfCancellationRequested();

            foreach (var userResult in userResults)
            {
                userResult.Avatar = $"{setting.ApiUrl}/Files/Images/{userResult.Avatar}";
                userResult.GroupFrom =  $"来自：{userResult.GroupFrom}";
                UserResults.Add(userResult);
            }

            var groupResults = await sugarClient.Queryable<ChatGroup>()
                .InnerJoin<UserContact>((cg, uc) => cg.Id == uc.ContactId && uc.IsGroup && uc.DeleteMark == 0)
                .InnerJoin<UserContactGroup>((cg, uc, ucg) => uc.UserContactGroupId == ucg.Id && ucg.IsGroup)
                .Select((cg, uc, ucg) => new UserContactItem
                {
                    ContactIdentifier = cg.GroupNum,
                    ContactName = cg.Name,
                    Avatar = cg.Avatar,
                    GroupFrom = ucg.Name
                })
                .Where(e=>e.ContactName.Contains(searchText)
                          || e.GroupFrom.Contains(searchText)
                          || e.ContactIdentifier.Contains(searchText))
                .ToListAsync(token);

            token.ThrowIfCancellationRequested();

            foreach (var groupResult in groupResults)
            {
                groupResult.Avatar = $"{setting.ApiUrl}/Files/Images/{groupResult.Avatar}";
                groupResult.GroupFrom =  $"来自：{groupResult.GroupFrom}";
                GroupResults.Add(groupResult);
            }
        }
        catch (OperationCanceledException)
        {
            // 搜索被取消，静默处理
        }
    }
    
    [RelayCommand]
    private void ShowAdding()
    {
        IsAdding = true;
    }
    
    [RelayCommand]
    private void CancelAdding()
    {
        IsAdding = false;
    }

    [RelayCommand]
    private void OpenComprehensiveSearch()
    {
        IsSearching = false;
        windowFactory.GetAndShowWindow<ComprehensiveSearch>(Window.GetWindow(View));
    }
}