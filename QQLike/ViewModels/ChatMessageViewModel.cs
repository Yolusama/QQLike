using System.Collections.ObjectModel;
using System.Net.Sockets;
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
using QQLike.Services;
using QQLike.Views.Message;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class ChatMessageViewModel(
    ISqlSugarClient sugarClient,
    ISessionStorage sessionStorage,
    IApiService apiService,
    SysSetting setting) : ViewModelBase<ChatMessageView>
{
    [ObservableProperty]
    private ObservableCollection<ChatHeadMessageItem> _headMessages = [];
    [ObservableProperty]
    private ChatHeadMessageItem? _selectedHeadMessage;
    [ObservableProperty]
    private ObservableCollection<ChatMessageItem> _chatMessages = [];
    [ObservableProperty]
    private bool _hasSelection;
    [ObservableProperty] 
    private bool _isNoSelection = true;
    
    private Socket? _client = null;

    [RelayCommand]
    private async Task LoadData()
    {
        var window = Window.GetWindow(View);
        if (_client == null)
        {
            var viewModel = window.GetViewModel<MainViewModel>();
            _client = viewModel.Client;
        }

        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var res = await apiService.GetAsync<List<V_HeadMessage>>
                ($"api/{nameof(HeadMessage)}/Get/{user.UserId}",null);

            if (!res.Success)
            {
                MessageComponent.ShowMessage(window, $"加载会话列表失败：{res.Message}", MessageType.Error);
                return;
            }
            HeadMessages.Clear();

            foreach (var header in res.Data)
            {
                var displayName = string.IsNullOrWhiteSpace(header.Remark) ? header.ContactName : header.Remark;
                var hasAvatar = !string.IsNullOrWhiteSpace(header.Avatar);
                HeadMessages.Add(new ChatHeadMessageItem
                {
                    ContactId = header.ContactId,
                    DisplayName = displayName,
                    LastContent = header.Content ?? string.Empty,
                    TimeText = FormatMessageTime(header.LastMessageTime),
                    Avatar = hasAvatar ? $"{setting.ApiUrl}/Files/Images/{header.Avatar}" : string.Empty,
                    HasAvatar = hasAvatar,
                    AvatarInitial = hasAvatar
                        ? string.Empty
                        : (string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Trim().Substring(0, 1)),
                    UnreadCount = header.UnreadCount,
                    HeadMessageId = header.HeadMessageId
                });
            }

            if (sessionStorage.KeyExists(CachingKeys.ChatMessageCurrentHeadId))
            {
                var currentHeadId = sessionStorage.Get<string>(CachingKeys.ChatMessageCurrentHeadId);
                SelectedHeadMessage = HeadMessages.FirstOrDefault(h=>h.HeadMessageId 
                == currentHeadId);
                await sugarClient.Updateable<ChatMessage>()
                    .SetColumns(e => e.IsRead == true)
                    .Where(e => e.UserId == user.UserId && e.ContactId == SelectedHeadMessage.ContactId && !e.IsRead)
                    .ExecuteCommandAsync();
                sessionStorage.Remove(CachingKeys.ChatMessageCurrentHeadId);
                HasSelection = true;
                IsNoSelection = false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(window, $"加载会话列表失败：{e.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    private async Task CheckMessages(ChatHeadMessageItem? item)
    {
        if(item == null)return;
        SelectedHeadMessage = item;
        HasSelection = true;
        IsNoSelection = false;
        ChatMessages.Clear();
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        var messages = await sugarClient.Queryable<ChatMessage>()
            .InnerJoin<V_HeadMessage>((c,v)=>c.HeadMessageId == v.HeadMessageId)
            .Where((c,v)=>c.HeadMessageId == item.HeadMessageId)
            .OrderByDescending((c,v)=>c.CreateTime)
            .Select((c,v)=>new ChatMessageItem
            {
                Avatar = v.Avatar,
                DisplayName = v.ContactName,
                Content = c.Content,
                MessageTime = c.CreateTime,
                UserId = c.UserId,
                ContactId = c.ContactId
            })
            .ToListAsync();
        foreach (var message in messages)
        {
            message.Avatar = $"{setting.ApiUrl}/Files/Images/{message.Avatar}";
            message.IsSelf = user.UserId == message.UserId;
            ChatMessages.Add(message);
        }

        View.ContentWriteTo.Focus();
    }
    

    private static string FormatMessageTime(DateTime? time)
    {
        if (time == null)
            return string.Empty;

        var value = time.Value;
        var today = DateTime.Today;

        if (value.Date == today)
            return value.ToString("HH:mm");
        
        if(value.Date == today.AddDays(-1))
            return "昨天";

        if (value.Date == today.AddDays(-2))
            return "前天";

        if (value.Date >= today.AddDays(-6))
            return value.DayOfWeek switch
            {
                DayOfWeek.Monday => "星期一",
                DayOfWeek.Tuesday => "星期二",
                DayOfWeek.Wednesday => "星期三",
                DayOfWeek.Thursday => "星期四",
                DayOfWeek.Friday => "星期五",
                DayOfWeek.Saturday => "星期六",
                _ => "星期日"
            };

        return value.ToString("yyyy/MM/dd");
    }
}