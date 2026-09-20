using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using QQLike.Components;
using QQLike.Domain;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.DTO;
using QQLike.Entity.Enum;
using QQLike.Entity.Exceptions;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services;
using QQLike.Services.Interfaces;
using QQLike.Views.Message;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class ChatMessageViewModel(
    ISqlSugarClient sugarClient,
    ISessionStorage sessionStorage,
    IUserChatSourceHandler sourceHandler,
    IRandomGenerator generator,
    IApiService apiService,
    IRabbitMQProducer mqProducer,
    IWindowFactory windowFactory,
    SysSetting setting) : ViewModelBase<ChatMessageView>, IDisposable
{
    private static readonly TimeSpan SocketSendTimeout = TimeSpan.FromSeconds(8);

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

    private CancellationTokenSource _downloadCancellationTokenSource = new();
    private CancellationTokenSource _uploadCancellationTokenSource = new();
    private readonly SemaphoreSlim _uploadSGate = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _downloadSGate = new SemaphoreSlim(1, 1);
    private Task? _uploadTask;
    private Task? _downloadTask;

    private long _uploadBytes = 0;
    private long _receivedBytes = 0;
    private const int LoadInterval = 10;
    private const int CalculateSpeedInterval = LoadInterval * 50;


    public string DefaultFileIcon => sourceHandler.ImageUrl("default-file-icon.png");

    private Socket? _client = null;
    private readonly SemaphoreSlim _sendGate = new(1, 1);

    private Socket? Client => GetSocket();

    private Socket? GetSocket()
    {
        if (_client != null) return _client;
        var window = Window.GetWindow(View);
        var viewModel = window.GetViewModel<MainViewModel>();
        _client = viewModel.Client;
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
                ($"api/{nameof(HeadMessage)}/Get/{user.UserId}", null);

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
            if (offlineReceiveCount > 0)
                await mqProducer.Produce(nameof(HeadMessage), Constants.MQExchange,
                    $"{nameof(HeadMessage)}_{user.UserId}",
                    messageBody.ToNormalJson());

            var unreadCount = res.Data.Sum(h => h.UnreadCount);
            if (unreadCount > 0)
            {
                messageBody.Muted = false;
                await mqProducer.Produce(nameof(HeadMessage), Constants.MQExchange,
                    $"{nameof(HeadMessage)}_{user.UserId}",
                    messageBody.ToNormalJson());
            }

            if (sessionStorage.KeyExists(CachingKeys.ChatMessageCurrentHeadId))
            {
                var currentHeadId = sessionStorage.Get<string>(CachingKeys.ChatMessageCurrentHeadId);
                SelectedHeadMessage = HeadMessages.FirstOrDefault(h => h.HeadMessageId
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
        using var cts = new CancellationTokenSource();
        try
        {
            if (item == null) return;
            SelectedHeadMessage = item;
            HasSelection = true;
            IsNoSelection = false;
            ChatMessages.Clear();
            var isGroup = item.IsGroup;
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            if (!isGroup)
            {
                var messages = await sugarClient.Queryable<V_UserChatMessage>()
                    .Where(v => v.UserId == user.UserId && v.HeadMessageId == item.HeadMessageId)
                    .OrderBy(v => v.CreateTime)
                    .ToListAsync(cts.Token);

                foreach (var message in messages)
                {
                    var type = (ChatMessageType)message.MessageType;
                    var contactName = string.IsNullOrEmpty(message.Remark) ? message.NickName : message.Remark;
                    var newMessageItem = new ChatMessageItem
                    {
                        MessageId = message.MessageId,
                        Avatar = sourceHandler.ImageUrl(message.Avatar),
                        DisplayName = contactName,
                        Content = message.Content,
                        MessageType = type,
                        FileName = message.FileName,
                        MessageTime = message.CreateTime,
                        UserId = message.UserId,
                        ContactId = message.ContactId,
                        IsSelf = message.IsSelf,
                        LocalSourcePath = message.LocalSourcePath,
                        DisplayFileName = message.OriginalFileName,
                        TempFileName = message.TempFileName,
                        Current = message.CurrentChunk,
                        Total = message.TotalChunkCount,
                        FileSize = message.FileSize,
                        IsBigFile = (type != ChatMessageType.Text && type != ChatMessageType.Notification) && FileTransmission.NeedTask(message.FileSize ?? 0),
                        FileSizeText = FileTransmission.GetMemoryText(message.FileSize ?? 0),
                        TransType = message.FileTransType == null ? null : (FileTransType)message.FileTransType,
                        ProcessState = message.ProcessState.HasValue ? (FileTransmissionState)message.ProcessState.Value : null,
                        MessageTimeText = FormatMessageTime(message.CreateTime, true),
                        ContactNameVisibility = Visibility.Collapsed
                    };
                    if (type != ChatMessageType.Text && type != ChatMessageType.Notification)
                    {
                        if (type == ChatMessageType.Image)
                        {
                            if (string.IsNullOrEmpty(message.LocalSourcePath) || !File.Exists(message.LocalSourcePath))
                            {
                                var fileBytes = await GetRemoteFileSource(message.FileName, type);
                                var chatMessage = message.MapTo(new ChatMessage());
                                chatMessage.FileBytes = fileBytes;
                                var newFileName = await DownloadAction(chatMessage, cts.Token);
                                message.LocalSourcePath = newFileName;
                                newMessageItem.LocalSourcePath = newFileName;
                            }
                        }

                        if (message.IsSelf)
                        {
                            newMessageItem.SourceDownloaded = true;
                            if (FileTransmission.NeedTask(message.FileSize ?? 0))
                            { 
                               newMessageItem.TaskId = message.TaskId;
                               newMessageItem.SourceUnload = 
                                   newMessageItem.ProcessState  == FileTransmissionState.Paused;
                               newMessageItem.SourceLoading = false;
                               if (newMessageItem.ProcessState == FileTransmissionState.Cancelled)
                               {
                                   newMessageItem.ProcessText = "文件传输任务已取消";
                                   newMessageItem.SourceUnload = false;
                               }
                            }
                            else
                                newMessageItem.SourceUnload = false;
                        }
                        else
                        {
                            newMessageItem.SourceDownloaded = File.Exists(message.LocalSourcePath);
                            newMessageItem.SourceUnload = !newMessageItem.SourceDownloaded;
                        }
                    }

                    if (newMessageItem.IsBigFile &&
                        newMessageItem.ProcessState == FileTransmissionState.Processing)
                    {
                        ChatMessages.Add(newMessageItem);
                        newMessageItem.SourceUnload = true;
                        await PauseLoadFile(newMessageItem);
                        await StartLoadFile(newMessageItem);
                    }
                    else
                       ChatMessages.Add(newMessageItem);
                }
            }
            else
            {
                var messages = await sugarClient.Queryable<V_ChatGroupMessage>()
                    .Where(v => v.UserId == user.UserId && v.HeadMessageId == item.HeadMessageId)
                    .OrderBy(v => v.CreateTime)
                    .ToListAsync(cts.Token);

                foreach (var message in messages)
                {
                    var type = (ChatMessageType)message.MessageType;
                    var contactName = string.IsNullOrEmpty(message.GroupDisplayName)
                        ? message.NickName
                        : message.GroupDisplayName;
                    var newMessageItem = new ChatMessageItem
                    {
                        MessageId = message.MessageId,
                        Avatar = sourceHandler.ImageUrl(message.Avatar),
                        DisplayName = contactName,
                        Content = message.Content,
                        MessageType = type,
                        FileName = message.FileName,
                        MessageTime = message.CreateTime,
                        UserId = message.UserId,
                        ContactId = message.ContactId,
                        GroupMemberId = message.GroupMemberId,
                        IsSelf = message.IsSelf,
                        IsOwner = message.IsOwner,
                        LocalSourcePath = message.LocalSourcePath,
                        DisplayFileName = message.OriginalFileName,
                        TempFileName = message.TempFileName,
                        Current = message.CurrentChunk,
                        Total = message.TotalChunkCount,
                        FileSize = message.FileSize,
                        IsBigFile = type != ChatMessageType.Text && FileTransmission.NeedTask(message.FileSize ?? 0),
                        FileSizeText = FileTransmission.GetMemoryText(message.FileSize ?? 0),
                        ProcessState = message.ProcessState.HasValue ? (FileTransmissionState)message.ProcessState.Value : null,
                        TransType = message.FileTransType == null ? null : (FileTransType)message.FileTransType,
                        MessageTimeText = FormatMessageTime(message.CreateTime, true),
                        ContactNameVisibility = Visibility.Visible
                    };
                    if (type != ChatMessageType.Text && type != ChatMessageType.Notification)
                    {
                        if (type == ChatMessageType.Image)
                        {
                            if (string.IsNullOrEmpty(message.LocalSourcePath) || !File.Exists(message.LocalSourcePath))
                            {
                                var fileBytes = await GetRemoteFileSource(message.FileName, type);
                                var chatMessage = message.MapTo(new ChatMessage());
                                chatMessage.FileBytes = fileBytes;
                                var newFileName = await DownloadAction(chatMessage, cts.Token);
                                message.LocalSourcePath = newFileName;
                                newMessageItem.LocalSourcePath = newFileName;
                            }
                        }

                        if (message.IsSelf)
                        {
                            newMessageItem.SourceDownloaded = true;
                            if (FileTransmission.NeedTask(message.FileSize ?? 0))
                            {
                                newMessageItem.SourceUnload =
                                    newMessageItem.ProcessState == FileTransmissionState.Paused;
                                newMessageItem.SourceLoading = false;
                            }
                            else
                            {
                                newMessageItem.SourceUnload = false;
                            }
                        }
                        else
                        {
                            newMessageItem.SourceDownloaded = File.Exists(message.LocalSourcePath);
                            newMessageItem.SourceUnload = !newMessageItem.SourceDownloaded;
                        }
                    }

                    ChatMessages.Add(newMessageItem);
                }
            }

            using var scope = sugarClient.CopyNew();
            await scope.Updateable<ChatMessage>()
                .SetColumns(e => e.IsRead == true)
                .Where(e => e.HeadMessageId == item.HeadMessageId && e.UserId == user.UserId && !e.IsRead)
                .ExecuteCommandAsync(cts.Token);
            item.UnreadCount = 0;

            var messageBody = new MQMessageBody
            {
                Identifier = user.UserId,
                Muted = true
            };
            await mqProducer.Produce(nameof(HeadMessage), Constants.MQExchange, $"{nameof(HeadMessage)}_{user.UserId}",
                messageBody.ToNormalJson());
            View.ContentWriteTo.Focus();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"加载消息记录失败：{e.Message}", MessageType.Error);
            await cts.CancelAsync();
        }
    }


    [RelayCommand]
    private async Task SendMessage()
    {
        foreach (var block in View.ContentWriteTo.Document.Blocks)
        {
            if (block is Paragraph paragraph)
            {
                foreach (var inline in paragraph.Inlines)
                {
                    switch (inline)
                    {
                        case Run run:
                            NewMessageText = run.Text;
                            await HandMessageSending(ChatMessageType.Text);
                            break;

                        case InlineUIContainer container when container.Child is Image image:
                        {
                            var bitmap = (BitmapImage)image.Source;
                            var fileInfo = new FileInfo(bitmap.UriSource.LocalPath);
                            await HandMessageSending(ChatMessageType.Image,new FileTypeMessageDTO
                            {
                                TempMessage = ChatMessageType.Image.FileTypeContent(),
                                FileName = Path.GetFileNameWithoutExtension(fileInfo.Name),
                                LocalFilePath = fileInfo.FullName,
                                OriginalFileName = fileInfo.Name,
                                FileSize = fileInfo.Length,
                                FileBytes =await fileInfo.ReadBytes(),
                                FileExtension = fileInfo.Extension
                            });
                        } break;
                    }
                }
            }
        }
        
        View.ContentWriteTo.Document.Blocks.Clear();
        
    }

    private async Task HandMessageSending(ChatMessageType type, FileTypeMessageDTO? fileTypeMessageDto = null)
    {
        if (SelectedHeadMessage == null)
        {
            return;
        }

        var socket = Client;
        if (socket is null || !socket.Connected)
        {
            MessageComponent.ShowMessage(Owner, "连接未就绪，请稍后重试", MessageType.Warning);
            return;
        }

        await _sendGate.WaitAsync();
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            var model = new ChatMessageTransModel();
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
            if (SelectedHeadMessage.IsGroup)
                message.GroupMemberId = user.UserId;
            if (type != ChatMessageType.Text && fileTypeMessageDto != null)
            {
                message.Content = fileTypeMessageDto.TempMessage;
                message.FileBytes = fileTypeMessageDto.FileBytes;
                message.FileName = fileTypeMessageDto.FileName + fileTypeMessageDto.FileExtension;
                message.OriginalFileName = fileTypeMessageDto.OriginalFileName;
                message.LocalSourcePath = fileTypeMessageDto.LocalFilePath;
                model.Message = fileTypeMessageDto.TempMessage;
                message.FileSize = fileTypeMessageDto.FileSize;
            }
            else
                model.Message = NewMessageText;

            model.Type = type;

            using var worker = sugarClient.CreateContext();
            try
            {
                message.IsOnline = true;
                var id = await worker.Db.Insertable(message).ExecuteReturnBigIdentityAsync();
                message.Id = id;
                model.Data = message;
                var json = JsonSerializer.Serialize(model);
                using var sendCts = new CancellationTokenSource(SocketSendTimeout);
                await socket.SendWith(Encoding.UTF8.GetBytes(json), sendCts.Token);
                NewMessageText = string.Empty;
                await sugarClient.Updateable<HeadMessage>()
                    .SetColumns(e => new HeadMessage
                        { Content = message.Content, LastMessageTime = message.CreateTime })
                    .Where(e => e.Id == SelectedHeadMessage.HeadMessageId)
                    .ExecuteCommandAsync(sendCts.Token);
                var messageBody = new MQMessageBody();
                messageBody.Identifier = message.ContactId;
                messageBody.Body = true;
                await mqProducer.Produce(nameof(HeadMessage), Constants.MQExchange,
                    $"{nameof(HeadMessage)}_{messageBody.Identifier}", JsonSerializer.Serialize(messageBody));
                messageBody.Body = new HeadMessageMQModel
                {
                    HeadMessageId = SelectedHeadMessage.HeadMessageId,
                    UserId = message.ContactId,
                    ContactId = message.UserId,
                    Content = message.Content,
                    LastMessageTime = message.CreateTime
                };
                await mqProducer.Produce(nameof(ChatMessage), Constants.MQExchange,
                    $"{nameof(ChatMessage)}_{messageBody.Identifier}", JsonSerializer
                        .Serialize(messageBody));
                var userContact = await sugarClient.Queryable<UserContact>()
                    .Where(e => e.UserId == user.UserId && e.ContactId == SelectedHeadMessage.ContactId)
                    .FirstAsync(sendCts.Token);
                var displayName = SelectedHeadMessage.IsGroup
                    ? (string.IsNullOrEmpty(userContact.GroupDisplayName)
                        ? user.Nickname
                        : userContact.GroupDisplayName)
                    : user.Nickname;
                var messageItem = new ChatMessageItem
                {
                    MessageId = message.Id,
                    Avatar = sourceHandler.ImageUrl(user.Avatar),
                    DisplayName = displayName,
                    Content = message.Content,
                    MessageType = type,
                    MessageTime = message.CreateTime,
                    UserId = message.UserId,
                    ContactId = message.ContactId,
                    IsSelf = true,
                    MessageTimeText = FormatMessageTime(message.CreateTime, true)
                };
                if (!SelectedHeadMessage.IsGroup)
                    messageItem.ContactNameVisibility = Visibility.Collapsed;
                if (messageItem.MessageType != ChatMessageType.Text)
                {
                    messageItem.FileName = message.FileName;
                    messageItem.LocalSourcePath = message.LocalSourcePath;
                    message.Content = fileTypeMessageDto?.TempMessage ?? message.Content;
                    messageItem.DisplayFileName = message.OriginalFileName;
                    messageItem.SourceDownloaded = true;
                    messageItem.SourceUnload = false;
                }

                ChatMessages.Add(messageItem);
                SelectedHeadMessage.LastContent = message.Content;
                SelectedHeadMessage.TimeText = FormatMessageTime(message.CreateTime);

                View.ContentWriteTo.Focus();
                worker.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MessageComponent.ShowMessage(Owner, $"发送消息失败：{e.Message}", MessageType.Error);
            }
        }
        finally
        {
            _sendGate.Release();
        }
    }

    public async Task UpdateHeadMessage(HeadMessageMQModel model)
    {
        try
        {
            var item = HeadMessages.FirstOrDefault(e => e.HeadMessageId == model.HeadMessageId);
            if (item == null)
                return;
            item.UnreadCount = await sugarClient.Queryable<ChatMessage>()
                .Where(e => e.HeadMessageId == model.HeadMessageId && !e.IsRead && e.UserId == model.UserId)
                .CountAsync();
            item.LastContent = model.Content;
            item.TimeText = FormatMessageTime(model.LastMessageTime);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"更新会话列表失败：{e.Message}", MessageType.Error);
        }
    }

    public async Task WriteMessage(ChatMessage message)
    {
        using var cancelTokenSource = new CancellationTokenSource();
        try
        {
            var headItem = HeadMessages.FirstOrDefault(e => e.HeadMessageId == message.HeadMessageId);

            if (headItem == null)
            {
                var newHeadMessageItem = new ChatHeadMessageItem
                {
                    HeadMessageId = message.HeadMessageId,
                    ContactId = message.ContactId,
                    DisplayName = headItem.DisplayName,
                    LastContent = message.Content,
                    TimeText = FormatMessageTime(message.CreateTime),
                    Avatar = sourceHandler.ImageUrl(headItem.Avatar),
                    UnreadCount = 1,
                    IsGroup = headItem.IsGroup
                };
                HeadMessages.Insert(0, newHeadMessageItem);
                headItem = newHeadMessageItem;
            }
            else
            {
                headItem.LastContent = message.Content;
                headItem.TimeText = FormatMessageTime(message.CreateTime);
                if (SelectedHeadMessage?.HeadMessageId != headItem.HeadMessageId)
                    headItem.UnreadCount += 1;
            }

            var isCurrentHeadSelected = SelectedHeadMessage?.HeadMessageId == headItem.HeadMessageId;
            if (!isCurrentHeadSelected) return;
            if (!headItem.IsGroup)
            {
                var contactUser = await sugarClient.Queryable<User>()
                    .LeftJoin<UserContact>((u, uc) => uc.UserId == u.Id)
                    .Where((u, uc) => u.Id == message.ContactId)
                    .Select((u, uc) => new { u.Nickname, u.Avatar, uc.Remark })
                    .FirstAsync(cancelTokenSource.Token);

                var messageType = (ChatMessageType)message.MessageType;
                var item = new ChatMessageItem
                {
                    MessageId = message.Id,
                    Avatar = sourceHandler.ImageUrl(contactUser.Avatar),
                    DisplayName = string.IsNullOrEmpty(contactUser.Remark) ? contactUser.Nickname : contactUser.Remark,
                    Content = message.Content,
                    MessageType = messageType,
                    MessageTime = message.CreateTime,
                    UserId = message.UserId,
                    ContactId = message.ContactId,
                    IsSelf = message.IsSelf,
                    FileName = message.FileName,
                    LocalSourcePath = message.LocalSourcePath,
                    FileSize = message.FileSize,
                    IsBigFile = messageType == ChatMessageType.File && FileTransmission.NeedTask(message.FileSize ?? 0),
                    DisplayFileName = message.OriginalFileName,
                    MessageTimeText = FormatMessageTime(message.CreateTime, true),
                    ContactNameVisibility = Visibility.Collapsed
                };
                if (messageType == ChatMessageType.Image)
                {
                    item.SourceDownloaded = true;
                    item.SourceUnload = false;
                }

                ChatMessages.Add(item);
            }
            else
            {
                var chatGroupContact = await sugarClient.Queryable<ChatGroup>()
                    .LeftJoin<UserContact>((c, uc) => c.Id == uc.ContactId && uc.IsGroup)
                    .InnerJoin<User>((c, uc, u) => uc.UserId == u.Id)
                    .Where((c, uc, u) => uc.UserId == message.GroupMemberId)
                    .Where((c, uc, u) => c.Id == message.ContactId)
                    .Select((c, uc, u) => new
                        { GroupName = c.Name, uc.GroupDisplayName, uc.Remark, u.Avatar, UserName = u.Nickname })
                    .FirstAsync(cancelTokenSource.Token);

                var messageType = (ChatMessageType)message.MessageType;
                var item = new ChatMessageItem
                {
                    MessageId = message.Id,
                    Avatar = sourceHandler.ImageUrl(chatGroupContact.Avatar),
                    DisplayName = string.IsNullOrEmpty(chatGroupContact.GroupDisplayName)
                        ? chatGroupContact.UserName
                        : chatGroupContact.GroupDisplayName,
                    Content = message.Content,
                    MessageType = messageType,
                    FileName = message.FileName,
                    LocalSourcePath = message.LocalSourcePath,
                    FileSize = message.FileSize,
                    IsBigFile = messageType == ChatMessageType.File && FileTransmission.NeedTask(message.FileSize ?? 0),
                    MessageTime = message.CreateTime,
                    UserId = message.GroupMemberId,
                    ContactId = message.ContactId,
                    IsSelf = message.IsSelf,
                    DisplayFileName = message.OriginalFileName,
                    MessageTimeText = FormatMessageTime(message.CreateTime, true),
                    ContactNameVisibility = Visibility.Visible
                };
                if (messageType == ChatMessageType.Image)
                {
                    item.SourceDownloaded = true;
                    item.SourceUnload = false;
                }

                ChatMessages.Add(item);
            }

            message.IsRead = true;

            if (message.MessageType == ChatMessageType.Image.GetValue())
                await DownloadAction(message, cancelTokenSource.Token);

            await sugarClient.Updateable<ChatMessage>()
                .SetColumns(e => e.IsRead == true)
                .Where(e => e.Id == message.Id)
                .ExecuteCommandAsync(cancelTokenSource.Token);

            var msgBody = new MQMessageBody
            {
                Identifier = message.UserId,
                Body = true,
                Muted = headItem.MessageReceiveMuted
            };
            await mqProducer.Produce(nameof(HeadMessage), Constants.MQExchange,
                $"{nameof(HeadMessage)}_{message.UserId}",
                msgBody.ToNormalJson());
        }
        catch (Exception e)
        {
            await cancelTokenSource.CancelAsync();
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"出现异常���{e.Message}", MessageType.Error);
        }
    }

    public void LoadHeadMessageAfterCreatingGroup(GroupCreatedHeadMessage headMessage)
    {
        var headMessageItem = new ChatHeadMessageItem();
        headMessageItem.HeadMessageId = headMessage.HeadMessageId;
        headMessageItem.ContactId = headMessage.GroupId;
        headMessageItem.UnreadCount = 0;
        headMessageItem.IsGroup = true;
        headMessageItem.Avatar = sourceHandler.ImageUrl(headMessage.GroupAvatar);
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

    [RelayCommand]
    private void OpenScreenShotComponent()
    {
        var window = windowFactory.GetWindow<ScreenShotComponent>();
        window.Background= new SolidColorBrush(Colors.Transparent);
        window.Show();
    }

    [RelayCommand]
    private async Task OpenFileDialog(string filter)
    {
        
        var dialog = new OpenFileDialog();
        dialog.Multiselect = true;
        dialog.Filter = filter;
        dialog.Title = "请选择";
        var result = dialog.ShowDialog();
        if (result != null && result.Value)
        {
            var fileNames = dialog.FileNames;
            foreach (var fileName in fileNames)
            {
                var fileInfo = new FileInfo(fileName);
                if (!FileTransmission.NeedTask(fileInfo.Length))
                {
                    var dto = new FileTypeMessageDTO
                    {
                        FileName = generator.Guid,
                        OriginalFileName = fileInfo.Name,
                        FileExtension = fileInfo.Extension,
                        LocalFilePath = fileInfo.FullName,
                        FileSize = fileInfo.Length,
                        FileBytes = await fileInfo.ReadBytes()
                    };

                    var type = EnumHelper.ToChatMessageType(fileInfo.Extension);
                    dto.TempMessage = type.FileTypeContent();
                    await HandMessageSending(type, dto);
                }
                else
                    _uploadTask = PrepareToUploadFile(generator.Guid + fileInfo.Extension, fileInfo.FullName,
                        fileInfo.Length);
            }
        }
    }
    

    [RelayCommand]
    private async Task StartLoadFile(ChatMessageItem? item)
    {
        if (item == null) return;

        await sugarClient.Updateable<FileTransmissionTask>()
            .SetColumns(e => e.State == FileTransmissionState.Processing.GetValue())
            .Where(e => e.Id == item.TaskId)
            .ExecuteCommandAsync();
        item.ProcessState = FileTransmissionState.Processing;
        item.SourceLoading = true;
        //item.SourceUnload = false;

        if (item.TransType == FileTransType.Upload)
        {
            _uploadBytes = 0;
            _uploadCancellationTokenSource.Dispose();
            _uploadCancellationTokenSource = new CancellationTokenSource();
            _uploadTask = PrepareToUploadFile(item.FileName, item.LocalSourcePath, item.FileSize.GetValueOrDefault());
        }

        if (item.TransType == null || item.TransType == FileTransType.Download)
        {
            _receivedBytes = 0;
            _downloadCancellationTokenSource.Dispose();
            _downloadCancellationTokenSource = new CancellationTokenSource();
            await PrepareToDownloadFile(item);
        }
    }
    

    [RelayCommand]
    private async Task PauseLoadFile(ChatMessageItem? item)
    {
        if (item == null) return;
        if (item.TransType == FileTransType.Upload)
        {
            await _uploadCancellationTokenSource.CancelAsync();
            _uploadBytes = 0;
        }

        if (item.TransType == FileTransType.Download)
        {
            await _downloadCancellationTokenSource.CancelAsync();
            _receivedBytes = 0;
        }

        await sugarClient.Updateable<FileTransmissionTask>()
            .SetColumns(e => e.State == FileTransmissionState.Paused.GetValue())
            .Where(e => e.Id == item.TaskId)
            .ExecuteCommandAsync();
        item.ProcessState = FileTransmissionState.Paused;
        item.SourceLoading = false;
        item.SourceUnload = true;
        item.SpeedText = string.Empty;
    }
    
    [RelayCommand]
    private async Task CancelLoadFile(ChatMessageItem? item)
    {
        if (item == null) return;
        if (item.TransType == FileTransType.Download)
        {
            await _downloadCancellationTokenSource.CancelAsync();
            _downloadCancellationTokenSource = new CancellationTokenSource();
            await sourceHandler.RemoveTemp(new FileTypeMessageModel
            {
                Type = item.MessageType, FileName = item.TempFileName
            },_downloadCancellationTokenSource.Token);
            _receivedBytes = 0;
        }

        if (item.TransType == FileTransType.Upload)
        {
            await _uploadCancellationTokenSource.CancelAsync();
            _uploadBytes = 0;
            await RemoveTempFile(item.TempFileName, item.MessageType);
        }
        
        await sugarClient.Updateable<FileTransmissionTask>()
            .SetColumns(e => e.State == FileTransmissionState.Cancelled.GetValue())
            .Where(e => e.Id == item.TaskId)
            .ExecuteCommandAsync();
        item.ProcessState = FileTransmissionState.Cancelled;
        item.SourceLoading = false;
        item.SourceUnload = true;
        item.SpeedText = string.Empty;
        item.ProcessText = "文件传输任务已取消";
    }

    [RelayCommand]
    private async Task Download(ChatMessageItem? item)
    {
        if (item == null) return;
        using var cts = new CancellationTokenSource();
        try
        {
            var message = await sugarClient.Queryable<ChatMessage>()
                .FirstAsync(m => m.Id == item.MessageId, cts.Token);
            var bytes = await GetRemoteFileSource(message.FileName, (ChatMessageType)message.MessageType);
            message.FileBytes = bytes;
            var localPath = await DownloadAction(message, cts.Token);
            MessageComponent.ShowMessage(Owner, "文件已保存", MessageType.Success);
            item.LocalSourcePath = localPath;
            item.SourceDownloaded = true;
            item.SourceUnload = false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"程序出现异常：{e.Message}", MessageType.Error);
            await cts.CancelAsync();
        }
        //接收文件，生成随机名称作为本地储存
    }

    [RelayCommand]
    private void OpenInFileBrowser(ChatMessageItem? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.LocalSourcePath) || !File.Exists(item.LocalSourcePath))
        {
            MessageComponent.ShowMessage(Owner, "文件已移动或者已被删除", MessageType.Error);
            return;
        }

        try
        {
            var path = item.LocalSourcePath.Replace('/', '\\');
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")
            {
                UseShellExecute = true
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"打开文件位置失败：{e.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    private void SendingTextChanged()
    {
        Thread.Sleep(LoadInterval);
        CanSendMessage = View.ContentWriteTo.HasContent();
    }

    private async Task<string> DownloadAction(ChatMessage? message, CancellationToken token)
    {
        var extension = Path.GetExtension(message.FileName) ?? string.Empty;
        var localPath = await sourceHandler.Receive(new FileTypeMessageModel
        {
            FileName = generator.Guid + extension,
            FileBytes = message.FileBytes,
            Type = (ChatMessageType)message.MessageType
        }, token);
        var fileTransmission = new FileTransmission();
        fileTransmission.IsReceiveSide = true;
        fileTransmission.FileName = message.FileName;
        fileTransmission.MessageId = message.Id;
        fileTransmission.HeadMessageId = message.HeadMessageId;
        fileTransmission.CreateTime = DateTime.Now;
        await sugarClient.Insertable(fileTransmission).ExecuteCommandAsync(token);
        await sugarClient.Updateable<ChatMessage>()
            .SetColumns(e => e.LocalSourcePath == localPath)
            .Where(e => e.Id == message.Id)
            .ExecuteCommandAsync(token);
        return localPath;
    }

    private async Task<byte[]> GetRemoteFileSource(string fileName, ChatMessageType type)
    {
        var headers = new Dictionary<string, string>();
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        headers.Add("Authorization", $"Bearer {user.Token}");
        try
        {
            var urlBuilder = new UriBuilder($"{setting.ApiUrl}/api/ChatMessage/GetMessageFileSource");
            var query = HttpUtility.ParseQueryString(urlBuilder.Query);
            query[nameof(type)] = ((int)type).ToString();
            query[nameof(fileName)] = fileName;
            urlBuilder.Query = query.ToString();
            var byteArr = await apiService.HttpService.GetFileResult(urlBuilder.ToString(), null, headers);
            return byteArr;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"获取远程文件源失败：{e.Message}", MessageType.Error);
            return [];
        }
    }

    private async Task PrepareToUploadFile(string fileName, string localPath, long fileSize)
    {
        var headers = new Dictionary<string, string>();
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        headers.Add("Authorization", $"Bearer {user.Token}");
        var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var cts = _uploadCancellationTokenSource;
        using var worker = sugarClient.CreateContext();
        try
        {
            var messageType = EnumHelper.ToChatMessageType(Path.GetExtension(fileName));
            var namePart = Path.GetFileNameWithoutExtension(fileName);
            var originalName = Path.GetFileName(localPath);
            var tempFileName = namePart + Constants.TempFileSuffix;
            var messageItem = ChatMessages.FirstOrDefault(e => e.UserId == user.UserId
                                                               && e.FileName == fileName);
            FileTransmissionTask fileTransmissionTask = null;
            ChatMessage chatMessage = null;
            FileTransmission fileTransmission = null;
            if (messageItem == null)
            {
                chatMessage = new ChatMessage();
                chatMessage.FileName = fileName;
                chatMessage.MessageType = messageType.GetValue();
                chatMessage.Content = messageType.FileTypeContent();
                chatMessage.OriginalFileName = originalName;
                chatMessage.LocalSourcePath = localPath;
                chatMessage.CreateTime = DateTime.Now;
                chatMessage.IsOnline = true;
                chatMessage.IsRead = true;
                chatMessage.IsSelf = true;
                chatMessage.UserId = user.UserId;
                chatMessage.ContactId = SelectedHeadMessage.ContactId;
                chatMessage.HeadMessageId = SelectedHeadMessage.HeadMessageId;
                chatMessage.FileSize = fileSize;
                messageItem = new ChatMessageItem();
                if (SelectedHeadMessage.IsGroup)
                {
                    chatMessage.GroupMemberId = user.UserId;
                    var groupDisplayName = await sugarClient.Queryable<UserContact>()
                        .Where(e => e.IsGroup && e.UserId == user.UserId &&
                                    e.ContactId == SelectedHeadMessage.ContactId)
                        .Select(e => e.GroupDisplayName)
                        .FirstAsync(cts.Token);
                    messageItem.DisplayName = string.IsNullOrEmpty(groupDisplayName)
                        ? user.Nickname
                        : groupDisplayName;
                }

                messageItem.FileName = fileName;
                messageItem.DisplayFileName = originalName;
                messageItem.Content = messageType.FileTypeContent();
                messageItem.MessageType = messageType;
                messageItem.LocalSourcePath = localPath;
                messageItem.Avatar = sourceHandler.ImageUrl(user.Avatar);
                messageItem.UserId = user.UserId;
                messageItem.FileSize = fileSize;
                messageItem.ContactId = SelectedHeadMessage.ContactId;
                messageItem.IsSelf = true;
                messageItem.IsBigFile = true;
                messageItem.FileSizeText = FileTransmission.GetMemoryText(fileSize);
                messageItem.MessageTime = chatMessage.CreateTime;
                messageItem.SourceUnload = true;
                messageItem.ProcessState = FileTransmissionState.Processing;
                var messageId = await worker.Db.Insertable(chatMessage).ExecuteReturnBigIdentityAsync(cts.Token);
                messageItem.MessageId = messageId;
                fileTransmissionTask = new FileTransmissionTask
                {
                    Current = 1,
                    Total = FileTransmission.GetTotal(fileSize),
                    TempFileName = tempFileName,
                    CreateTime = DateTime.Now,
                    State = FileTransmissionState.Processing.GetValue(),
                    Type = FileTransType.Upload.GetValue()
                };
                var taskId = await worker.Db
                    .Insertable(fileTransmissionTask)
                    .ExecuteReturnBigIdentityAsync(cts.Token);
                fileTransmissionTask.Id = taskId;
                messageItem.TaskId = taskId;

                fileTransmission = new FileTransmission
                {
                    TaskId = taskId,
                    HeadMessageId = SelectedHeadMessage.HeadMessageId,
                    MessageId = messageId,
                    IsValid = true,
                    IsReceiveSide = false,
                    FileName = fileName,
                    CreateTime = DateTime.Now
                };
                await worker.Db.Insertable(fileTransmission).ExecuteCommandAsync(cts.Token);
                ChatMessages.Add(messageItem);
                await worker.Db.Updateable<HeadMessage>()
                    .SetColumns(e => new HeadMessage
                    {
                        Content = messageType.FileTypeContent(),
                        LastMessageTime = messageItem.MessageTime
                    })
                    .Where(e => e.Id == SelectedHeadMessage.HeadMessageId)
                    .ExecuteCommandAsync(cts.Token);
            }
            else
            {
                       
                fileTransmission = await sugarClient.Queryable<FileTransmission>()
                    .Where(f=>f.TaskId == messageItem.TaskId)
                    .FirstAsync(cts.Token);
                if (!fileTransmission.IsValid)
                {
                    MessageComponent.ShowMessage(Owner,"文件已无效",MessageType.Warning);
                    return;
                }
                fileTransmissionTask = await sugarClient.Queryable<FileTransmissionTask>()
                    .Where(f => f.Id == messageItem.TaskId)
                    .FirstAsync(cts.Token);
                
                chatMessage = await sugarClient.Queryable<ChatMessage>()
                    .Where(c => c.Id == messageItem.MessageId)
                    .FirstAsync(cts.Token);
            }

            worker.Commit();

            var bufferSize = FileTransmission.GetBufferSize(fileSize);
            if (fileTransmissionTask.Current < fileTransmissionTask.Total)
                messageItem.SourceLoading = true;
            while (fileTransmissionTask.Current <= fileTransmissionTask.Total)
            {
                if (cts.IsCancellationRequested) break;
               var beginTicks = await Task.Run(async () =>
                {
                    await _uploadSGate.WaitAsync(cts.Token);
                    try
                    {
                        if (cts.IsCancellationRequested) return Constants.EOF;
                        var beginTicks = DateTime.Now.Ticks;
                        using var content = new MultipartFormDataContent();
                        var position = (fileTransmissionTask.Current - 1) * bufferSize;
                        var buffer = new byte[bufferSize];
                        fileStream.Seek(position, SeekOrigin.Begin);
                        var bytesRead = await fileStream.ReadAsync(buffer, cts.Token);
                        content.Add(new ByteArrayContent(buffer.Take(bytesRead).ToArray()), "file", fileName);
                        content.Add(new StringContent(messageItem.TaskId.ToString()), "taskId");
                        content.Add(new StringContent(fileTransmissionTask.Current.ToString()), "current");
                        content.Add(new StringContent(fileTransmissionTask.Total.ToString()), "total");
                        content.Add(new StringContent(bufferSize.ToString()), "bufferSize");
                        content.Add(new StringContent(tempFileName), nameof(tempFileName));
                        content.Add(
                            new StringContent(
                                ((int)EnumHelper.ToChatMessageType(Path.GetExtension(fileName))).ToString()),
                            "messageType");
                        var resStr = await apiService.HttpService.Request
                            ($"{setting.ApiUrl}/api/ChatMessage/UploadFile", HttpMethod.Post, content, headers);
                        var res = JsonSerializer.Deserialize<ResponseResult<bool>>(resStr,
                            Constants.DesSerializerOptions);
                        if (res.Success)
                        {
                            if (res.Data)
                            {
                                this.UIDispatch(async () =>
                                {
                                    NotificationComponent.ShowNotification(Owner, "文件上传完成", NotificationType.Success);
                                    var data = JsonSerializer.Serialize(chatMessage.ToNormalJson());
                                    await Client.SendAsync(Encoding.UTF8.GetBytes(data), cts.Token);
                                });
                            }

                            fileTransmissionTask.Current += 1;
                            messageItem.Current = fileTransmissionTask.Current;
                            messageItem.ProcessText = fileTransmissionTask.PercentStr();
                            messageItem.LoadProgress =
                                Math.Round(fileTransmissionTask.Current * 100d / fileTransmissionTask.Total, 1);
                            _uploadBytes += bytesRead;
                            if (fileTransmissionTask.Current == fileTransmissionTask.Total)
                            {
                                messageItem.ProcessState = FileTransmissionState.Finished;
                                messageItem.SourceDownloaded = true;
                                messageItem.SourceUnload = false;
                                messageItem.ProcessText = string.Empty;
                                messageItem.SourceLoading = false;
                            }

                            await Task.Delay(LoadInterval, cts.Token);
                            return beginTicks;
                        }
                        else
                        {
                            MessageComponent.ShowMessage(Owner, $"上传文件失败：{res.Message}", MessageType.Error);
                            throw new ServiceException(res.Message);
                        }
                    }
                    catch (Exception e)
                    {
                        if (e is TaskCanceledException) return Constants.EOF;
                        Console.WriteLine(e);
                        throw new ServiceException(e.Message);
                    }
                    finally
                    {
                        _uploadSGate.Release();
                    }
                }, cts.Token);
               if(beginTicks == Constants.EOF)
                   return;
               else
               {
                 await Task.Run(async () =>
                   {
                       await Task.Delay(CalculateSpeedInterval, cts.Token);
                       var endTicks = DateTime.Now.Ticks;
                       var milliseconds = (endTicks - beginTicks) * 1.0d / TimeSpan.TicksPerMillisecond - CalculateSpeedInterval - LoadInterval;
                       var bytesPerSecond =(long)Math.Round(_uploadBytes * 1000d / milliseconds, 0);
                       messageItem.SpeedText = FileTransmission.GetMemoryText(bytesPerSecond) + "/s";
                       _uploadBytes = 0;
                   },cts.Token);
               }
            }
        }
        catch (Exception e)
        {
            if(e is TaskCanceledException)return;
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"准备上传文件失败：{e.Message}", MessageType.Error);
            await cts.CancelAsync();
        }
        finally
        {
            fileStream.Close();
        }
    }

    private async Task PrepareToDownloadFile(ChatMessageItem? item)
    {
        if (item == null) return;
        var cts = _downloadCancellationTokenSource;
        using var worker = sugarClient.CreateContext();
        var bufferSize = FileTransmission.GetBufferSize(item.FileSize.GetValueOrDefault());
        try
        {
            var fileTransmissionTask = await sugarClient.Queryable<FileTransmissionTask>()
                .Where(f => f.Id == item.TaskId)
                .FirstAsync(cts.Token);
            var fileTransmission = await sugarClient.Queryable<FileTransmission>()
                .Where(f=>f.TaskId ==  item.TaskId)
                .FirstAsync(cts.Token);
            if (fileTransmissionTask == null)
            {
                var suffix = Path.GetExtension(item.FileName);
                fileTransmissionTask = new FileTransmissionTask
                {
                    Current = 1,
                    Total = FileTransmission.GetTotal(item.FileSize.GetValueOrDefault()),
                    TempFileName = string.IsNullOrEmpty(suffix)
                        ? Path.GetFileName(item.FileName) + suffix
                        : item.FileName.Substring(0, item.FileName.Length - suffix.Length) + Constants.TempFileSuffix,
                    CreateTime = DateTime.Now,
                    State = FileTransmissionState.Processing.GetValue(),
                    Type = FileTransType.Download.GetValue()
                };
                fileTransmissionTask.Id = await worker.Db
                    .Insertable(fileTransmissionTask)
                    .ExecuteReturnBigIdentityAsync(cts.Token);
                fileTransmission = new FileTransmission
                {
                    FileName = item.FileName,
                    IsReceiveSide = true,
                    IsValid = true,
                    CreateTime = DateTime.Now,
                    MessageId = item.MessageId,
                    HeadMessageId = SelectedHeadMessage.HeadMessageId,
                    TaskId = fileTransmissionTask.Id
                };
                await sugarClient.Insertable(fileTransmission)
                    .ExecuteCommandAsync(cts.Token);
                item.TaskId = fileTransmissionTask.Id;
                item.ProcessText = fileTransmissionTask.PercentStr();
                item.ProcessState = FileTransmissionState.Processing;
                item.TempFileName = fileTransmissionTask.TempFileName;
            }
            else
            {
                if (!fileTransmission.IsValid)
                {
                    MessageComponent.ShowMessage(Owner,"文件已失效", MessageType.Warning);
                    return;
                }
            }
            while (fileTransmissionTask.Current <= fileTransmissionTask.Total)
            {
                if(cts.IsCancellationRequested)
                    break;
                var beginTicks =  await Task.Run(async () =>
                {
                    await _downloadSGate.WaitAsync(cts.Token);
                    var beginTicks = DateTime.Now.Ticks;
                    if(cts.IsCancellationRequested)
                        return Constants.EOF;
                    try
                    {
                        var model = new MessageFileDownloadDTO
                        {
                            FileName = item.FileName,
                            Current = fileTransmissionTask.Current,
                            Total = fileTransmissionTask.Total,
                            BufferSize = bufferSize
                        };
                        var res = await apiService.GetAsync<byte[]>
                            ($"{setting.ApiUrl}/api/ChatMessage/DownloadFile", model);
                        if (res.Success)
                        {
                            fileTransmissionTask.Current += 1;
                            item.Current = fileTransmissionTask.Current;
                            var finished = item.Current == fileTransmissionTask.Total;
                            var bytes = res.Data.Length;
                            _receivedBytes += bytes;
                            await sourceHandler.ReceivePart(new FileTypeMessageModel
                            {
                                FileName = item.TempFileName,
                                FileBytes = res.Data,
                                Type = item.MessageType
                            }, finished, cts.Token);
                            item.ProcessText = fileTransmissionTask.PercentStr();
                            if (finished)
                                item.ProcessState = FileTransmissionState.Finished;
                            await Task.Delay(LoadInterval, cts.Token);
                        }
                        else
                        {
                            MessageComponent.ShowMessage(Owner, $"上传下载失败：{res.Message}", MessageType.Error);
                            throw new ServiceException(res.Message);
                        }

                        return beginTicks;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw new ServiceException(e.Message);
                    }
                    finally
                    {
                        _downloadSGate.Release();
                    }
                }, cts.Token);
                if(beginTicks == Constants.EOF)
                    return;
                else
                {
                    await Task.Run(async () =>
                    {
                        await Task.Delay(CalculateSpeedInterval, cts.Token);
                        var endTicks = DateTime.Now.Ticks;
                        var milliseconds = (endTicks - beginTicks) * 1.0d / TimeSpan.TicksPerMillisecond - CalculateSpeedInterval - LoadInterval;
                        var bytesPerSecond =(long)Math.Round(_receivedBytes * 1000d / milliseconds, 0);
                        item.SpeedText = FileTransmission.GetMemoryText(bytesPerSecond) + "/s";
                        _receivedBytes = 0;
                    },cts.Token);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"准备上传文件失败：{e.Message}", MessageType.Error);
            await cts.CancelAsync();
        }
    }

    private async Task<bool> RemoveTempFile(string tempFileName, ChatMessageType type)
    {
        try
        {
            var res = await apiService.DeleteAsync<object>
                ("api/ChatMessage/RemoveTempFile", new { FileName = tempFileName,MessageType = type.GetValue()});
            if (res.Success)
                return true;
            else
            {
                MessageComponent.ShowMessage(Owner, $"删除临时文件失败：{res.Message}", MessageType.Error);
                return false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"删除临时文件失败：{e.Message}", MessageType.Error);
            return false;
        }
    }

    private static string FormatMessageTime(DateTime? time, bool isMessaging = false)
    {
        if (time == null)
            return string.Empty;

        var value = time.Value;
        var today = DateTime.Today;

        if (value.Date == today)
            return value.ToString("HH:mm");

        if (value.Date == today.AddDays(-1))
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

        return isMessaging ? value.ToString("yyyy/MM/dd HH:mm:ss") : value.ToString("yyyy/MM/dd");
    }

    public void Dispose()
    {
        _downloadCancellationTokenSource.SafeDispose();
        _uploadCancellationTokenSource.SafeDispose();
        _sendGate.SafeDispose();
        _uploadSGate.SafeDispose();
        _downloadSGate.SafeDispose();
        _uploadTask?.SafeDispose();
        _downloadTask?.SafeDispose();
    }
}