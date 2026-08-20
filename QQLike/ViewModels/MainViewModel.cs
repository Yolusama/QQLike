using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
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
using QQLike.Services;
using QQLike.Services.Interfaces;
using QQLike.Views;
using RabbitMQ.Client.Events;
using SqlSugar;
using Constants = QQLike.Entity.Common.Constants;
using MessageBoxOptions = QQLike.Entity.VO.MessageBoxOptions;

namespace QQLike.ViewModels;

public partial class MainViewModel(
    ISessionStorage sessionStorage,
    IRabbitMQConsumer mqConsumer,
    ISqlSugarClient sugarClient,
    SysSetting setting) : ViewModelBase<MainView>, IDisposable
{
    private static readonly TimeSpan ReceiveDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(3);
    private const int MaxReconnectAttempts = 3;

    [ObservableProperty] 
    private MDMenuItem? _selectedMenuItem;
    [ObservableProperty] 
    private string _audioSource;

    [ObservableProperty]
    private ObservableCollection<MDMenuItem> _menuItems =
    [
        new MDMenuItem
        {
            Key = nameof(ChatMessage), Title = "消息", SelectedIcon = PackIconKind.MessageText,
            UnselectedIcon = PackIconKind.MessageTextOutline
        },
        new MDMenuItem
        {
            Key = nameof(UserContact), Title = "联系人", SelectedIcon = PackIconKind.AccountGroup,
            UnselectedIcon = PackIconKind.AccountGroupOutline
        },
        new MDMenuItem
        {
            Key = nameof(VerificationMessage), Title = "验证消息", SelectedIcon = PackIconKind.MessageAlert,
            UnselectedIcon = PackIconKind.MessageAlertOutline
        },
        new MDMenuItem
        {
            Key = nameof(SysSetting), Title = "设置", SelectedIcon = PackIconKind.Cog,
            UnselectedIcon = PackIconKind.CogOutline
        }
    ];

    private readonly SemaphoreSlim _socketGate = new(1, 1);
    private readonly SemaphoreSlim _mqGate = new(1, 1);
    private readonly SemaphoreSlim _reconnectGate = new(1, 1);
    private readonly byte[] _receiveBuffer = new byte[4 * 1024];

    private Socket? _client;
    private Task? _receiveTask;
    private Task? _heartbeatTask;
    private CancellationTokenSource _cts = new();
    private bool _mqStarted;
    private bool _disposed;
    private bool _isReconnecting;
    private bool _isShutdownDialogShown;

    public Socket Client => _client;
    
    partial void OnAudioSourceChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                View.Player.Source = new Uri(value, UriKind.Relative);
                View.Player.Play();
            });
        }
    }

    [RelayCommand]
    private async Task ConnectSocketServer()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_receiveTask is null || _receiveTask.IsCompleted)
        {
            _receiveTask = Receive(_cts.Token);
        }

        if (_heartbeatTask is null || _heartbeatTask.IsCompleted)
        {
            _heartbeatTask = HeartbeatLoop(_cts.Token);
        }

        try
        {
            await ConnectSocketInternal(_cts.Token);
        }
        catch (Exception e) when (e is SocketException or ObjectDisposedException)
        {
            await StartReconnectFlow(_cts.Token);
        }
    }

    partial void OnSelectedMenuItemChanged(MDMenuItem? value)
    {
        if (value == null) return;
        SelectedMenuItem = value;
        foreach (var item in MenuItems)
            item.Activated = false;
        SelectedMenuItem.Activated = true;
    }

    public void ShowMenu(string key)
    {
        var menuItem = MenuItems.FirstOrDefault(e => e.Key == key);
        if (menuItem != null)
            SelectedMenuItem = menuItem;
    }

    [RelayCommand]
    private void OpenUserProfile()
    {
    }


    private async Task Receive(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var receiveBytes = new List<byte>();
            try
            {
                var client = _client;
                if (client is null || !client.Connected)
                {
                    await StartReconnectFlow(cancellationToken);
                    await Task.Delay(ReceiveDelay, cancellationToken);
                    continue;
                }

                int bytes;
                while (client.Available > 0)
                {
                    bytes = await client.ReceiveAsync(_receiveBuffer, SocketFlags.None, cancellationToken);
                    receiveBytes.AddRange(_receiveBuffer.Take(bytes));
                }

                if (receiveBytes.Count == 0)
                {
                    await Task.Delay(ReceiveDelay, cancellationToken);
                    continue;
                }

                var model = JsonSerializer.Deserialize<ChatMessageTransModel>(receiveBytes.ToArray());
                if (model.Type != ChatMessageType.Head && model.Type != ChatMessageType.Heartbeat)
                {
                    var chatViewModel = View.ChatMessageView.GetViewModel<ChatMessageViewModel>();
                    await chatViewModel.WriteMessage(
                        JsonSerializer.Deserialize<ChatMessage>(JsonSerializer.Serialize(model.Data)));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                await StartReconnectFlow(cancellationToken);
                await Task.Delay(ReceiveDelay, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Task.Delay(ReceiveDelay, cancellationToken);
            }
        }
    }

    [RelayCommand]
    private async Task StartMQConsuming()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _mqGate.WaitAsync();
        try
        {
            if (_mqStarted)
            {
                return;
            }

            await mqConsumer.Consume(nameof(VerificationMessage), Constants.MQExchange, nameof(VerificationMessage),
                ConsumeMessage);
            await mqConsumer.Consume(nameof(HeadMessage), Constants.MQExchange, nameof(HeadMessage), ConsumeMessage);
            await mqConsumer.Consume(nameof(ChatMessage), Constants.MQExchange, nameof(ChatMessage), ConsumeMessage);
            _mqStarted = true;
        }
        finally
        {
            _mqGate.Release();
        }
    }

    private async Task ConsumeMessage(object data, BasicDeliverEventArgs ea)
    {
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        var messageBody = JsonSerializer.Deserialize<MQMessageBody>(ea.Body.ToArray());
        if (user.UserId != messageBody.Identifier) return;
        if(!string.IsNullOrEmpty(AudioSource))
            AudioSource = string.Empty;

        // 创建线程独立的 SqlSugar 实例，避免多线程并发共享 SqlSugarScope 连接状态
        using var db = sugarClient.CopyNew();

        if (ea.RoutingKey == nameof(VerificationMessage))
        {
            var menuItem = MenuItems.FirstOrDefault(e => e.Key == nameof(VerificationMessage));
            if (menuItem is null) return;
            var unreadCount = await db.Queryable<VerificationMessage>()
                .Where(e => !e.IsRead && e.UserId == user.UserId)
                .CountAsync();
            if (unreadCount == 0) return;
            menuItem.Notification = unreadCount > 100 ? "99+" : unreadCount.ToString();
            AudioSource = "/Resource/Audio/verification.mp3";
        }

        if (ea.RoutingKey == nameof(HeadMessage))
        {
            var menuItem = MenuItems.FirstOrDefault(e => e.Key == nameof(ChatMessage));
            if (menuItem is null) return;
            var unreadCount = await db.Queryable<ChatMessage>()
                .Where(e => !e.IsRead && e.UserId == user.UserId)
                .CountAsync();
            if (unreadCount == 0) return;
            menuItem.Notification = unreadCount > 100 ? "99+" : unreadCount.ToString();
            AudioSource = "/Resource/Audio/message.mp3";
        }

        if (ea.RoutingKey == nameof(ChatMessage))
        {
            var chatViewModel = View.ChatMessageView.GetViewModel<ChatMessageViewModel>();
            var model = JsonSerializer.Deserialize<HeadMessageMQModel>(JsonSerializer.Serialize(messageBody.Body));
            await chatViewModel.UpdateHeadMessage(model);
        }
    }

    private static Socket CreateClient() =>
        new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

    private async Task ConnectSocketInternal(CancellationToken cancellationToken)
    {
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);

        await _socketGate.WaitAsync(cancellationToken);
        try
        {
            if (_client is { Connected: true })
            {
                return;
            }

            if (_client is not null)
            {
                CloseSocket(_client);
            }

            _client = CreateClient();
            await _client.ConnectAsync(setting.SocketUrl, setting.SocketServerPort, cancellationToken);

            var transModel = new ChatMessageTransModel
            {
                Type = ChatMessageType.Head,
                Data = user.UserId
            };
            await _client.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(transModel)), SocketFlags.None,
                cancellationToken);
        }
        finally
        {
            _socketGate.Release();
        }
    }

    private async Task StartReconnectFlow(CancellationToken cancellationToken)
    {
        if (_disposed || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        await _reconnectGate.WaitAsync(cancellationToken);
        try
        {
            if (_isReconnecting || _disposed)
            {
                return;
            }

            _isReconnecting = true;
        }
        finally
        {
            _reconnectGate.Release();
        }

        try
        {
            for (var attempt = 1; attempt <= MaxReconnectAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ConnectSocketInternal(cancellationToken);
                    return;
                }
                catch (Exception e) when (e is SocketException or ObjectDisposedException)
                {
                    Console.WriteLine($"Socket reconnect attempt {attempt}/{MaxReconnectAttempts} failed: {e.Message}");

                    if (attempt == MaxReconnectAttempts)
                    {
                        await ShowReconnectFailedDialogAndShutdown();
                        return;
                    }

                    await Task.Delay(ReconnectDelay, cancellationToken);
                }
            }
        }
        finally
        {
            _isReconnecting = false;
        }
    }

    private async Task ShowReconnectFailedDialogAndShutdown()
    {
        if (_isShutdownDialogShown || _disposed)
        {
            return;
        }

        _isShutdownDialogShown = true;

        MessageBoxComponent.ShowMessageBox(
            View,
            new MessageBoxOptions
            {
                ConfirmAction = Application.Current.Shutdown,
                CancelAction = _ => Application.Current.Shutdown(),
                ConfirmButtonText = "确定",
                CancelButtonText = "关闭",
                Title = "连接失败",
                Message = "服务器连接失败，已重试 3 次。点击确定后将关闭程序。",
            });
    }

    private void CloseSocket(Socket socket)
    {
        try
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
            }
        }
        catch
        {
            // Ignore cleanup exceptions during reconnect/dispose.
        }
        finally
        {
            socket.Dispose();
            if (ReferenceEquals(_client, socket))
            {
                _client = null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _cts.Cancel();
        if (_client is not null)
        {
            CloseSocket(_client);
        }

        _cts.Dispose();
        _socketGate.Dispose();
        _mqGate.Dispose();
        _reconnectGate.Dispose();
        mqConsumer.RemoveHandler();
    }

    [RelayCommand]
    private async Task ClosingApplication()
    {
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        await sugarClient.Updateable<User>()
            .SetColumns(e => e.IsOnline == false)
            .Where(e => e.Id == user.UserId)
            .ExecuteCommandAsync();
    }

    private async Task HeartbeatLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = _client;
                if (client is { Connected: true })
                {
                    var heartbeat = new ChatMessageTransModel
                    {
                        Type = ChatMessageType.Heartbeat,
                        Message = "Heartbeat"
                    };
                    var json = JsonSerializer.Serialize(heartbeat);
                    await client.SendAsync(Encoding.UTF8.GetBytes(json), SocketFlags.None, cancellationToken);
                }
                else
                {
                    await StartReconnectFlow(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine($"心跳发送失败：{e}");
                await StartReconnectFlow(cancellationToken);
            }

            try
            {
                await Task.Delay(HeartbeatInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}