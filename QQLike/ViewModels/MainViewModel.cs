using System.Collections.ObjectModel;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using QQLike.Domain;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Services.Interfaces;
using QQLike.Views;

namespace QQLike.ViewModels;

public partial class MainViewModel(ISessionStorage sessionStorage,
    SysSetting setting) : ViewModelBase<MainView>, IDisposable
{
    private static readonly TimeSpan ReceiveDelay = TimeSpan.FromSeconds(1);

    [ObservableProperty]
    private MDMenuItem? _selectedMenuItem;

    [ObservableProperty]
    private ObservableCollection<MDMenuItem> _menuItems =
    [
        new MDMenuItem { Title = "消息", SelectedIcon = PackIconKind.MessageText, UnselectedIcon = PackIconKind.MessageTextOutline },
        new MDMenuItem { Title = "联系人", SelectedIcon = PackIconKind.AccountGroup, UnselectedIcon = PackIconKind.AccountGroupOutline },
        new MDMenuItem { Title = "验证消息", SelectedIcon = PackIconKind.MessageAlert, UnselectedIcon = PackIconKind.MessageAlertOutline },
        new MDMenuItem { Title = "设置", SelectedIcon = PackIconKind.Cog, UnselectedIcon = PackIconKind.CogOutline }
    ];

    private readonly SemaphoreSlim _socketGate = new(1, 1);
    private readonly byte[] _receiveBuffer = new byte[4 * 1024];

    private Socket? _client;
    private Task? _receiveTask;
    private CancellationTokenSource _cts = new();
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
                if (bytes == 0)
                {
                    // Remote endpoint closed; keep loop alive for later reconnection.
                    CloseSocket(client);
                    await Task.Delay(ReceiveDelay, cancellationToken);
                }
                else
                {
                    // TODO: decode _receiveBuffer[..bytes] and dispatch the message.
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
                await Task.Delay(ReceiveDelay, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Task.Delay(ReceiveDelay, cancellationToken);
            }
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
    }
}
