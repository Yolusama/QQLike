using System.Collections.ObjectModel;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using QQLike.Domain;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Services.Interfaces;
using QQLike.Views;
using RabbitMQ.Client.Events;
using SqlSugar;
using Constants = QQLike.Entity.Common.Constants;

namespace QQLike.ViewModels;

public partial class MainViewModel(ISessionStorage sessionStorage,
    IRabbitMQConsumer mqConsumer,
    ISqlSugarClient sugarClient,
    SysSetting setting) : ViewModelBase<MainView>, IDisposable
{
    private static readonly TimeSpan ReceiveDelay = TimeSpan.FromSeconds(1);

    [ObservableProperty]
    private MDMenuItem? _selectedMenuItem;

    [ObservableProperty]
    private ObservableCollection<MDMenuItem> _menuItems =
    [
        new MDMenuItem { Key = nameof(ChatMessage), Title = "消息", SelectedIcon = PackIconKind.MessageText, UnselectedIcon = PackIconKind.MessageTextOutline },
        new MDMenuItem { Key = nameof(UserContact), Title = "联系人", SelectedIcon = PackIconKind.AccountGroup, UnselectedIcon = PackIconKind.AccountGroupOutline },
        new MDMenuItem { Key = nameof(VerificationMessage), Title = "验证消息", SelectedIcon = PackIconKind.MessageAlert, UnselectedIcon = PackIconKind.MessageAlertOutline },
        new MDMenuItem { Key = nameof(SysSetting), Title = "设置", SelectedIcon = PackIconKind.Cog, UnselectedIcon = PackIconKind.CogOutline }
    ];

    private readonly SemaphoreSlim _socketGate = new(1, 1);
    private readonly SemaphoreSlim _mqGate = new(1, 1);
    private readonly byte[] _receiveBuffer = new byte[4 * 1024];

    private Socket? _client;
    private Task? _receiveTask;
    private CancellationTokenSource _cts = new();
    private bool _mqStarted;
    private bool _disposed;

    [RelayCommand]
    private async Task ConnectSocketServer()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _ = sessionStorage.Get<UserLoginVO>(CachingKeys.User);

        await _socketGate.WaitAsync();
        try
        {
            if (_client is { Connected: true })
            {
                return;
            }

            _cts.Dispose();
            _cts = new CancellationTokenSource();

            if (_client is not null)
            {
                CloseSocket(_client);
            }

            _client = CreateClient();
            await _client.ConnectAsync(setting.SocketUrl, setting.SocketServerPort);

            if (_receiveTask is null || _receiveTask.IsCompleted)
            {
                _receiveTask = Receive(_cts.Token);
            }
        }
        finally
        {
            _socketGate.Release();
        }
    }

    partial void OnSelectedMenuItemChanged(MDMenuItem? value)
    {
        if (value == null) return;
        SelectedMenuItem = value;
        foreach (var item in MenuItems)
            item.Activated = false;
        SelectedMenuItem.Activated = true;
        SwitchPage(value.Title);
    }

    private void SwitchPage(string title)
    {
        switch (title)
        {
            case "消息":
                break;
            case "联系人":
            case "验证消息":
                break;
            case "设置":
                break;
        }
    }

    [RelayCommand]
    private void OpenUserProfile()
    {
    }
    

    private async Task Receive(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = _client;
                if (client is null || !client.Connected)
                {
                    await Task.Delay(ReceiveDelay, cancellationToken);
                    continue;
                }

                var bytes = await client.ReceiveAsync(_receiveBuffer, SocketFlags.None, cancellationToken);
               
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

            await mqConsumer.Consume(nameof(VerificationMessage), Constants.MQExchange, nameof(VerificationMessage), ConsumeMessage);
            await mqConsumer.Consume(nameof(ChatMessage), Constants.MQExchange, nameof(ChatMessage), ConsumeMessage);
            _mqStarted = true;
        }
        finally
        {
            _mqGate.Release();
        }
    }
    private async Task ConsumeMessage(object data,BasicDeliverEventArgs ea)
    {
        if (ea.RoutingKey == nameof(VerificationMessage))
        {
            var menuItem = MenuItems.FirstOrDefault(e => e.Key == nameof(VerificationMessage));
            if (menuItem is null) return;
            var unreadCount =await sugarClient.Queryable<VerificationMessage>()
                .Where(e=>!e.IsRead)
                .CountAsync();
            menuItem.Notification = unreadCount.ToString();
        }

        if (ea.RoutingKey == nameof(ChatMessage))
        {
            var menuItem = MenuItems.FirstOrDefault(e => e.Key == nameof(ChatMessage));
            if (menuItem is null) return;
            var unreadCount = await sugarClient.Queryable<ChatMessage>()
                //.Where(e=>!e.IsRead)
                .CountAsync();
            menuItem.Notification = unreadCount.ToString();
        }
    }

    private static Socket CreateClient() =>
        new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

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
        mqConsumer.RemoveHandler();
    }

    [RelayCommand]
    private async Task ClosingApplication()
    { 
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        await sugarClient.Updateable<User>()
            .SetColumns(e => e.IsOnline == false)
            .Where(e=>e.Id == user.UserId)
            .ExecuteCommandAsync();
    }
}
