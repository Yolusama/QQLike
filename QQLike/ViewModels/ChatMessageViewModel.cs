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
    [ObservableProperty]
    private bool _isUserCardPopupOpen;
    [ObservableProperty] 
    private bool _canSendMessage; 
    [ObservableProperty]
    private bool _isUserContactOpen;

    private string _fileName = string.Empty;
    private Socket? _client = null;

    private Socket? Client => GetSocket();

    private Socket? GetSocket()
    {
        if(_client != null) return _client;
        var window = Window.GetWindow(View);
        var viewModel = window.GetViewModel<MainViewModel>();
        _client = viewModel.Client;
        return viewModel.Client;
    }
    
    partial void OnNewMessageTextChanged(string value)
    {
        CanSendMessage = value.Length > 0;
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
                    HeadMessageId = header.HeadMessageId,
                    IsGroup = header.IsGroup,
                    IsOwner = header.IsOwner,
                    MessageReceiveMuted = header.MessageReceiveMuted
                });
            }
            
            var headMessageIds = res.Data.Select(h => h.HeadMessageId).ToList();
            var offlineReceiveCount = await sugarClient.Queryable<ChatMessage>()
                .Where(e => e.UserId == user.UserId && headMessageIds.Contains(e.HeadMessageId) && !e.IsOnline)
                .CountAsync();
            var messageBody = new MQMessageBody
            {
                Identifier = user.UserId,
                Muted = true
            };
            if(offlineReceiveCount > 0)
               await mqProducer.Produce(nameof(HeadMessage),Constants.MQExchange,$"{nameof(HeadMessage)}_{user.UserId}",
                    messageBody.ToNormalJson());
            
            var unreadCount = res.Data.Sum(h => h.UnreadCount);
            if (unreadCount > 0)
            {
                messageBody.Muted = false;
                await mqProducer.Produce(nameof(HeadMessage),Constants.MQExchange,$"{nameof(HeadMessage)}_{user.UserId}",
                    messageBody.ToNormalJson());
            }

            if (sessionStorage.KeyExists(CachingKeys.ChatMessageCurrentHeadId))
            {
                var currentHeadId = sessionStorage.Get<string>(CachingKeys.ChatMessageCurrentHeadId);
                SelectedHeadMessage = HeadMessages.FirstOrDefault(h=>h.HeadMessageId 
                == currentHeadId);
                await sugarClient.Updateable<ChatMessage>()
                    .SetColumns(e => e.IsRead == true)
                    .Where(e => e.HeadMessageId == currentHeadId && e.UserId == user.UserId && !e.IsRead)
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
            var isGroup = item.IsGroup;
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var messages = await sugarClient.Queryable<V_UserChatMessage>()
                .Where(v=>v.UserId == user.UserId && v.HeadMessageId == item.HeadMessageId)
                .OrderBy(v=>v.CreateTime)
                .ToListAsync();
            foreach (var message in messages)
            {
                var type = (ChatMessageType)message.MessageType;
                var contactName = string.IsNullOrEmpty(message.Remark) ? message.NickName : message.Remark;
                ChatMessages.Add(new ChatMessageItem
                {
                    Avatar = $"{setting.ApiUrl}/Files/Images/{message.Avatar}",
                    DisplayName =  contactName,
                    Content = message.Content,
                    MessageType = type,
                    FileName = message.FileName,
                    MediaUrl = BuildMediaUrl(type, message.Content),
                    MessageTime = message.CreateTime,
                    UserId = message.UserId,
                    ContactId = message.ContactId,
                    IsSelf = message.IsSelf,
                    MessageTimeText = FormatMessageTime(message.CreateTime,true),
                    ContactNameVisibility = isGroup ? Visibility.Visible : Visibility.Collapsed
                });
            }
            
            await worker.Db.Updateable<ChatMessage>()
                .SetColumns(e => e.IsRead == true)
                .Where(e => e.HeadMessageId == item.HeadMessageId && e.UserId == user.UserId && !e.IsRead) 
                .ExecuteCommandAsync();
            item.UnreadCount = 0;
            // Commit first so MQ consumers can immediately see the latest unread count.
            worker.Commit();

            var messageBody = new MQMessageBody
            {
                Identifier = user.UserId,
                Muted = true
            };
            await mqProducer.Produce(nameof(HeadMessage),Constants.MQExchange,$"{nameof(HeadMessage)}_{user.UserId}",
                messageBody.ToNormalJson());
            View.ContentWriteTo.Focus();
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
        await HandMessageSending(ChatMessageType.Text);
    }
    
    private async Task HandMessageSending(ChatMessageType type)
    {
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        var message = new ChatMessage
        {
            UserId = user.UserId,
            ContactId = SelectedHeadMessage.ContactId,
            Content = NewMessageText,
            MessageType = type.GetValue(),
            CreateTime = DateTime.Now,
            HeadMessageId = SelectedHeadMessage.HeadMessageId,
            IsRead = true,
            IsSelf = true
        };
        if(SelectedHeadMessage.IsGroup)
            message.GroupMemberId = user.UserId;
        using var worker = sugarClient.CreateContext();
        try
        {
            var model = new ChatMessageTransModel();
            model.Type = type;
            model.Message = NewMessageText;
            message.IsOnline = true;
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
            messageBody.Body = true;
            await mqProducer.Produce(nameof(HeadMessage),Constants.MQExchange,$"{nameof(HeadMessage)}_{messageBody.Identifier}",JsonSerializer.Serialize(messageBody));
            messageBody.Body = new HeadMessageMQModel
            {
                HeadMessageId = SelectedHeadMessage.HeadMessageId,
                UserId = message.ContactId,
                ContactId = message.UserId
            };
            await mqProducer.Produce(nameof(ChatMessage),Constants.MQExchange,$"{nameof(ChatMessage)}_{messageBody.Identifier}",JsonSerializer
                .Serialize(messageBody));
            var userContact = await sugarClient.Queryable<UserContact>()
                .Where(e => e.UserId == user.UserId && e.ContactId == SelectedHeadMessage.ContactId)
                .FirstAsync();
            var displayName = SelectedHeadMessage.IsGroup ? userContact.GroupDisplayName : user.Nickname;
            var messageItem = new ChatMessageItem
            {
                Avatar = $"{setting.ApiUrl}/Files/Images/{user.Avatar}",
                DisplayName = displayName,
                Content = message.Content,
                MessageType = ChatMessageType.Text,
                FileName = _fileName,
                MediaUrl = string.Empty,
                MessageTime = message.CreateTime,
                UserId = message.UserId,
                ContactId = message.ContactId,
                IsSelf = true,
                MessageTimeText = FormatMessageTime(message.CreateTime, true)
            };
            if(!SelectedHeadMessage.IsGroup)
                messageItem.ContactNameVisibility = Visibility.Collapsed;
            ChatMessages.Add(messageItem); 
           
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
                .Where(e => e.HeadMessageId == model.HeadMessageId && !e.IsRead && e.UserId == model.UserId)
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
                .LeftJoin<UserContact>((u,uc)=>uc.UserId == u.Id)
                .Where((u,uc) => u.Id == message.ContactId)
                .Select((u,uc) => new { u.Nickname, u.Avatar,uc.Remark,IsGroupContact = uc.IsGroup })
                .FirstAsync();
            var messageType = (ChatMessageType)message.MessageType;
            var item = new ChatMessageItem
            {
                Avatar = $"{setting.ApiUrl}/Files/Images/{contactUser.Avatar}",
                DisplayName = string.IsNullOrEmpty(contactUser.Remark) ? contactUser.Nickname : contactUser.Remark,
                Content = message.Content,
                MessageType = messageType,
                FileName = message.FileName,
                MediaUrl = BuildMediaUrl(messageType, message.Content),
                MessageTime = message.CreateTime,
                UserId = message.UserId,
                ContactId = message.ContactId,
                IsSelf = message.IsSelf,
                MessageTimeText = FormatMessageTime(message.CreateTime,true),
                ContactNameVisibility = contactUser.IsGroupContact ? Visibility.Visible : Visibility.Collapsed
            };
            ChatMessages.Add(item);

            var existsInDb = message.Id > 0 && await worker.Db.Queryable<ChatMessage>()
                .Where(e => e.Id == message.Id)
                .AnyAsync();

            if (!existsInDb)
            {
                var model = new HeadMessageModel
                {
                    UserId = message.UserId,
                    ContactId = message.ContactId,
                    Content = message.Content,
                    LastMessageTime = message.CreateTime
                };

                var res = await apiService.PutAsync<string>($"api/{nameof(HeadMessage)}/Create", model);
                if (!res.Success)
                {
                    MessageComponent.ShowMessage(Owner, res.Message, MessageType.Error);
                    return;
                }

                message.IsRead = false;
                message.HeadMessageId = res.Data;
                await worker.Db.Insertable(message).ExecuteCommandAsync();
            }

            var headItem = HeadMessages.FirstOrDefault(e => e.HeadMessageId == message.HeadMessageId);

            if (headItem == null)
            {
                var newHeadMessageItem = new ChatHeadMessageItem
                {
                    HeadMessageId = message.HeadMessageId,
                    ContactId = message.ContactId,
                    DisplayName = string.IsNullOrEmpty(contactUser.Remark) ? contactUser.Nickname : contactUser.Remark,
                    LastContent = message.Content,
                    TimeText = FormatMessageTime(message.CreateTime),
                    Avatar = $"{setting.ApiUrl}/Files/Images/{contactUser.Avatar}",
                    HasAvatar = true,
                    AvatarInitial = string.Empty,
                    UnreadCount = 1,
                    IsGroup = contactUser.IsGroupContact
                };
                HeadMessages.Add(newHeadMessageItem);
                headItem =  newHeadMessageItem;
                
            }
            else
            {
                headItem.LastContent = message.Content;
                headItem.TimeText = FormatMessageTime(message.CreateTime);
                if (SelectedHeadMessage?.HeadMessageId != headItem.HeadMessageId)
                {
                    headItem.UnreadCount += 1;
                }
            }

            var msgBody = new MQMessageBody
            {
                Identifier = message.UserId,
                Body = true,
                Muted = headItem.MessageReceiveMuted
            };
            await mqProducer.Produce(nameof(HeadMessage), Constants.MQExchange, $"{nameof(HeadMessage)}_{message.UserId}",
                msgBody.ToNormalJson());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"出现异常：{e.Message}", MessageType.Error);
        }
        
    }

    public void LoadHeadMessageAfterCreatingGroup(GroupCreatedHeadMessage headMessage)
    {
        var headMessageItem = new ChatHeadMessageItem();
        headMessageItem.HeadMessageId = headMessage.HeadMessageId;
        headMessageItem.ContactId = headMessage.GroupId;
        headMessageItem.UnreadCount = 0;
        headMessageItem.IsGroup = true;
        headMessageItem.Avatar =  $"{setting.ApiUrl}/Files/Images/{headMessage.GroupAvatar}";
        headMessageItem.DisplayName = headMessage.GroupName;
        headMessageItem.LastContent = string.Empty;
        headMessageItem.TimeText = FormatMessageTime(headMessage.CreateTime);
        headMessageItem.IsOwner = headMessageItem.IsOwner;
        HeadMessages.Insert(0, headMessageItem);
    }
    
    [RelayCommand]
    private void OpenUserCardPopup()
    {
        IsUserCardPopupOpen = true;
        var cardViewModel = View.UserContactSimpleCard.GetViewModel<UserContactSimpleCardViewModel>();
        cardViewModel.IsGroup = SelectedHeadMessage.IsGroup;
        cardViewModel.UserId = SelectedHeadMessage.ContactId;
        cardViewModel.Visible = Visibility.Visible;
    }

    private static string FormatMessageTime(DateTime? time,bool isMessaging = false)
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

        return  isMessaging? value.ToString("yyyy/MM/dd HH:mm:ss") : value.ToString("yyyy/MM/dd");
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