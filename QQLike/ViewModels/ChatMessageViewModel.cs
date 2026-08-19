using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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
using QQLike.Entity.Model;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services;
using QQLike.Views.Message;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class ChatMessageViewModel(
    ISqlSugarClient sugarClient,
    ISessionStorage sessionStorage,
    IApiService apiService,
    IRabbitMQProducer mqProducer,
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
    [ObservableProperty]
    private string _newMessageText;
    
    private Socket? Client => GetSocket();

    private Socket? GetSocket()
    {
        var window = Window.GetWindow(View);
        var viewModel = window.GetViewModel<MainViewModel>();
        return viewModel.Client;
    }

    [RelayCommand]
    private async Task LoadData()
    {
        var window = Window.GetWindow(View);

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
        using var worker = sugarClient.CreateContext();
        try
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
                .Where((c,v)=>c.UserId == user.UserId || c.ContactId == user.UserId)
                .OrderByDescending((c,v)=>c.CreateTime)
                .Select((c,v)=>new ChatMessageVO
                {
                    Avatar = v.Avatar,
                    ContactId = c.ContactId,
                    Content = c.Content,
                    CreateTime = c.CreateTime,
                    FileName = c.FileName,
                    MessageType = c.MessageType,
                    UserId = c.UserId,
                    ContactName = v.ContactName
                })
                .ToListAsync();
            foreach (var message in messages)
            {
                var type = (ChatMessageType)message.MessageType;
                ChatMessages.Add(new ChatMessageItem
                {
                    Avatar = $"{setting.ApiUrl}/Files/Images/{message.Avatar}",
                    DisplayName = message.ContactName,
                    Content = message.Content,
                    MessageType = type,
                    FileName = message.FileName,
                    MediaUrl = BuildMediaUrl(type, message.Content),
                    MessageTime = message.CreateTime,
                    UserId = message.UserId,
                    ContactId = message.ContactId,
                    IsSelf = user.UserId == message.UserId
                });
            }
            
            await worker.Db.Updateable<ChatMessage>()
                .SetColumns(e => e.IsRead == true)
                .Where(e => e.UserId == user.UserId && e.ContactId == item.ContactId && !e.IsRead)
                .ExecuteCommandAsync();
            item.UnreadCount = 0;
            var messageBody = new MQMessageBody
            {
                Identifier = user.UserId
            };
            await mqProducer.Produce(nameof(HeadMessage),Constants.MQExchange,nameof(HeadMessage),
                JsonSerializer.Serialize(messageBody));
            View.ContentWriteTo.Focus();
            
            worker.Commit();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"加载消息记录失败：{e.Message}", MessageType.Error);
        }
        
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        if(string.IsNullOrEmpty(NewMessageText.TrimStart())) return;
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        var message = new ChatMessage
        {
            UserId = user.UserId,
            ContactId = SelectedHeadMessage.ContactId,
            Content = NewMessageText,
            MessageType = ChatMessageType.Text.GetValue(),
            CreateTime = DateTime.Now,
            HeadMessageId = SelectedHeadMessage.HeadMessageId,
            IsRead = true
        };
        using var worker = sugarClient.CreateContext();
        try
        {
            var model = new ChatMessageTransModel();
            model.Type = ChatMessageType.Text;
            model.Message = NewMessageText;
            var id = await worker.Db.Insertable(message).ExecuteReturnBigIdentityAsync();
            message.Id = id;
            model.Data = message;
            var json = JsonSerializer.Serialize(model);
            await Client.SendAsync(Encoding.UTF8.GetBytes(json), SocketFlags.None);
            NewMessageText = string.Empty;
            await sugarClient.Updateable<HeadMessage>()
                .SetColumns(e => new HeadMessage { Content = message.Content,LastMessageTime = message.CreateTime })
                .Where(e => e.Id == SelectedHeadMessage.HeadMessageId)
                .ExecuteCommandAsync();
            var messageBody = new MQMessageBody();
            messageBody.Identifier = message.ContactId;
            await mqProducer.Produce(nameof(HeadMessage),Constants.MQExchange,nameof(HeadMessage),JsonSerializer.Serialize(messageBody));
            messageBody.Body = new HeadMessageMQModel
            {
                HeadMessageId = SelectedHeadMessage.HeadMessageId,
                UserId = message.ContactId,
                ContactId = message.UserId
            };
            await mqProducer.Produce(nameof(ChatMessage),Constants.MQExchange,nameof(ChatMessage),JsonSerializer
                .Serialize(messageBody));
            ChatMessages.Add(new ChatMessageItem
            {
                Avatar = $"{setting.ApiUrl}/Files/Images/{user.Avatar}",
                DisplayName = user.Nickname,
                Content = message.Content,
                MessageType = ChatMessageType.Text,
                FileName = string.Empty,
                MediaUrl = string.Empty,
                MessageTime = message.CreateTime,
                UserId = message.UserId,
                ContactId = message.ContactId,
                IsSelf = true
            }); 
            View.ContentWriteTo.Focus();
            worker.Commit();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"发送消息失败：{e.Message}", MessageType.Error);
        }
    }
    
    public async Task UpdateHeadMessage(HeadMessageMQModel model)
    {
        try
        {
            var item = HeadMessages.FirstOrDefault(e => e.HeadMessageId == model.HeadMessageId);
            if(item == null)
                return;
            item.UnreadCount = await sugarClient.Queryable<ChatMessage>()    
                .Where(e => e.HeadMessageId == model.HeadMessageId && !e.IsRead && e.UserId == model.ContactId)
                .CountAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"更新会话列表失败：{e.Message}", MessageType.Error);
        }
    }

    public async Task WriteMessage(ChatMessage message)
    {
        using var worker =  sugarClient.CreateContext();
        try
        {
            var contactUser = await sugarClient.Queryable<User>()
                .Where(e => e.Id == message.ContactId)
                .Select(e => new { e.Nickname, e.Avatar })
                .FirstAsync();
            var item = new ChatMessageItem
            {
                Avatar = $"{setting.ApiUrl}/Files/Images/{contactUser.Avatar}",
                DisplayName = contactUser.Nickname,
                Content = message.Content,
                MessageType = (ChatMessageType)message.MessageType,
                FileName = message.FileName,
                MediaUrl = message.Content,
                MessageTime = message.CreateTime,
                UserId = message.UserId,
                ContactId = message.ContactId,
                IsSelf = false
            };
            ChatMessages.Add(item);
            var model = new HeadMessageModel()
            {
                UserId =  message.UserId,
                ContactId = message.ContactId,
                Content = message.Content,
                LastMessageTime = message.CreateTime
            };
            var res = await apiService
                .PutAsync<string>($"api/{nameof(HeadMessage)}/Create", model);
            if(!res.Success)
                MessageComponent.ShowMessage(Owner, res.Message, MessageType.Error);
            else
            {
                message.IsRead = false;
                message.HeadMessageId = res.Data;
                await worker.Db.Insertable(message).ExecuteCommandAsync();
                HeadMessages.Add(new ChatHeadMessageItem
                {
                    HeadMessageId = res.Data,
                    ContactId = message.ContactId,
                    DisplayName = contactUser.Nickname,
                    LastContent = message.Content,
                    TimeText = FormatMessageTime(message.CreateTime),
                    Avatar = $"{setting.ApiUrl}/Files/Images/{contactUser.Avatar}",
                    HasAvatar = true,
                    AvatarInitial = string.Empty,
                    UnreadCount = 1
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"出现异常：{e.Message}", MessageType.Error);
        }
        
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

    private string BuildMediaUrl(ChatMessageType type, string content)
    {
        if (type is ChatMessageType.Head or ChatMessageType.Heartbeat or ChatMessageType.Text)
            return string.Empty;

        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        return $"{setting.ApiUrl}/Files/{content.TrimStart('/')}";
    }
}