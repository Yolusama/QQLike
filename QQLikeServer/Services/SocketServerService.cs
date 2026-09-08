using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using QQLike.Entity;
using QQLike.Functional.Instructure;
using QQLike.Entity.Configuration.Server;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Functional.Utils;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class SocketServerService(
    SysSetting setting,
    IProjectLogger logger,
    IFreeSql orm,
    ISourceHandler sourceHandler,
    IRandomGenerator randomGenerator) : ISocketServerService
{
    private readonly ConcurrentDictionary<int, Socket> _temp = new();
    private readonly ConcurrentDictionary<string, Socket> _userSockets = new();
    private readonly ConcurrentDictionary<Socket, DateTime> _lastHeartbeat = new();
    private readonly ConcurrentDictionary<Socket, Task> _clientReceiveTasks = new();

    private readonly Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly CancellationTokenSource _tokenSource = new();
    private Task? _acceptLoopTask;
    private Task? _receiveLoopTask;
    private int _isStarted;
    private const int MaxQueueCount = 1000;
    private const int MaxMessageSize = 10 * 1024 * 1024;
    private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan ReceiveLoopInterval = TimeSpan.FromMilliseconds(100);

    public void Run()
    {
        if (Interlocked.Exchange(ref _isStarted, 1) == 1)
        {
            return;
        }

        _serverSocket.Bind(new IPEndPoint(IPAddress.Any, setting.ServerPort));
        _serverSocket.Listen(MaxQueueCount);
        Console.WriteLine($"聊天服务器已于端口{setting.ServerPort}上打开");
        logger.Log($"聊天服务器已于端口{setting.ServerPort}上打开", "聊天服务器");
        _acceptLoopTask = Task.Run(() => SocketThread(_tokenSource.Token));
        _receiveLoopTask = Task.Run(() => ReceiveThread(_tokenSource.Token));
    }

    private async Task SocketThread(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var socket = await _serverSocket.AcceptAsync(token);
                var port = (socket.RemoteEndPoint as IPEndPoint)?.Port;
                if (port == null) continue;
                _temp[port.Value] = socket;
                _lastHeartbeat[socket] = DateTime.UtcNow;
                _clientReceiveTasks[socket] = Task.Run(() => ReceiveClientLoop(port.Value, socket, token), token);

                //_sockets.TryAdd(ip.Port, socket);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                continue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"客户端连接出现异常：{ex}");
                await logger.LogAsync($"客户端连接出现异常：{ex}", "聊天服务器");
                continue;
            }
        }

        Console.WriteLine("客户端连接终止");
        await logger.LogAsync("客户端连接终止", "聊天服务器");
    }

    private async Task ReceiveClientLoop(int tempKey, Socket socket, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (!socket.Connected)
                {
                    CleanupSocket(tempKey, socket, "连接已断开");
                    break;
                }

                var model = await ReadFrameAsync(socket, token);
                if (model is null)
                {
                    CleanupSocket(tempKey, socket, "连接已断开");
                    break;
                }

                await ProcessIncomingModel(model, socket, token);
            }
        }
        catch (OperationCanceledException e)
        {
            // Expected during shutdown.
            //Console.WriteLine(e);
        }
        catch (SocketException)
        {
            CleanupSocket(tempKey, socket, "接收消息失败，已断开连接");
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            await logger.LogAsync($"消息处理异常：{ex}", "聊天服务器");
        }
        finally
        {
            _clientReceiveTasks.TryRemove(socket, out _);
        }
    }

    private async Task<ChatMessageTransModel?> ReadFrameAsync(Socket socket, CancellationToken token)
    {
        var lengthBuffer = new byte[sizeof(int)];
        var headerOk = await ReceiveExactAsync(socket, lengthBuffer, token);
        if (!headerOk)
        {
            return null;
        }

        var totalLength = BitConverter.ToInt32(lengthBuffer, 0);
        if (totalLength <= 0 || totalLength > MaxMessageSize)
        {
            throw new InvalidDataException($"非法消息长度：{totalLength}");
        }

        var payload = new byte[totalLength];
        var payloadOk = await ReceiveExactAsync(socket, payload, token);
        if (!payloadOk)
        {
            return null;
        }

        return JsonSerializer.Deserialize<ChatMessageTransModel>(payload);
    }

    private static async Task<bool> ReceiveExactAsync(Socket socket, byte[] buffer, CancellationToken token)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, buffer.Length - offset), SocketFlags.None, token);
            if (read == 0)
            {
                return false;
            }

            offset += read;
        }

        return true;
    }

    private async Task ProcessIncomingModel(ChatMessageTransModel model, Socket socket, CancellationToken token)
    {
        if (model.Type == ChatMessageType.Head)
        {
            var userId = ParseDataAsString(model.Data);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                _userSockets[userId] = socket;
            }

            _lastHeartbeat[socket] = DateTime.UtcNow;
            return;
        }

        if (model.Type == ChatMessageType.Heartbeat)
        {
            _lastHeartbeat[socket] = DateTime.UtcNow;
            Console.WriteLine($"收到心跳消息，来自{socket.RemoteEndPoint}");
            await logger.LogAsync($"收到心跳消息，来自{socket.RemoteEndPoint}", "聊天服务器");
            return;
        }

        var message = JsonSerializer.Deserialize<ChatMessage>(JsonSerializer.Serialize(model.Data));
        if (message is null)
        {
            return;
        }

        var isGroup = await orm.Select<UserContact>()
            .Where(e => e.UserId == message.UserId && e.ContactId == message.ContactId)
            .ToOneAsync(e => e.IsGroup, token);
        if (!isGroup)
        {
            var receiptMessage = await PersistUserMessage(message);
            if (receiptMessage == null) return;

            var contactId = message.ContactId;

            var transModel = model.MapTo(new ChatMessageTransModel());
            transModel.Data = receiptMessage;
            var data = JsonSerializer.Serialize(transModel);
            if (_userSockets.TryGetValue(contactId, out var contactSocket) && contactSocket.Connected)
            {
                await contactSocket.SendWith(Encoding.UTF8.GetBytes(data), token);
                receiptMessage.IsOnline = true;
                _lastHeartbeat[socket] = DateTime.UtcNow;
            }
            else
                receiptMessage.IsOnline = false;

            await orm.Update<ChatMessage>()
                .SetSource(receiptMessage)
                .UpdateColumns(c => c.IsOnline)
                .ExecuteAffrowsAsync(token);
        }
        else
        {
            var receiptMessages = await PersistGroupMessage(message);
            foreach (var receiptMessage in receiptMessages)
            {
                var transModel = model.MapTo(new ChatMessageTransModel());
                transModel.Data = receiptMessage;
                var data = JsonSerializer.Serialize(transModel);
                if (_userSockets.TryGetValue(receiptMessage.UserId, out var contactSocket) && contactSocket.Connected)
                {
                    await contactSocket.SendWith(Encoding.UTF8.GetBytes(data), token);
                    receiptMessage.IsOnline = true;
                    _lastHeartbeat[socket] = DateTime.UtcNow;
                }
                else
                    receiptMessage.IsOnline = false;

                await orm.Update<ChatMessage>()
                    .SetSource(receiptMessage)
                    .UpdateColumns(c => c.IsOnline)
                    .ExecuteAffrowsAsync(token);
            }
        }

        if (message.MessageType != ChatMessageType.Text.GetValue()
            && message.MessageType != ChatMessageType.Notification.GetValue())
        {
            await sourceHandler.Store(new FileTypeMessageModel
            {
                FileName = message.FileName,
                FileBytes = message.FileBytes,
                Type = (ChatMessageType)message.MessageType
            }, token);
            var transmission = new FileTransmission();
            transmission.FileName = message.FileName;
            transmission.MessageId = message.Id;
            transmission.HeadMessageId = message.HeadMessageId;
            transmission.IsValid = true;
            transmission.IsReceiveSide = false;
            transmission.CreateTime = DateTime.Now;
            await orm.Insert(transmission)
                .ExecuteAffrowsAsync(token);
        }
    }

    private async Task ReceiveThread(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            foreach (var kv in _temp.ToArray())
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var tempKey = kv.Key;
                var socket = kv.Value;

                if (!socket.Connected)
                {
                    CleanupSocket(tempKey, socket, "连接已断开");
                    continue;
                }

                if (_lastHeartbeat.TryGetValue(socket, out var lastHeartbeatAt)
                    && DateTime.UtcNow - lastHeartbeatAt > HeartbeatTimeout)
                {
                    CleanupSocket(tempKey, socket, "心跳超时，已断开连接");
                    continue;
                }
            }

            try
            {
                await Task.Delay(ReceiveLoopInterval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static string? ParseDataAsString(object? data)
    {
        if (data is null)
        {
            return null;
        }

        if (data is string text)
        {
            return text;
        }

        if (data is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }

            return element.GetRawText();
        }

        return data.ToString();
    }

    private async Task<ChatMessage?> PersistUserMessage(ChatMessage senderMessage)
    {
        using var worker = orm.CreateUnitOfWork();
        try
        {
            var senderId = senderMessage.UserId;
            var recipientId = senderMessage.ContactId;
            var createTime = senderMessage.CreateTime ?? DateTime.Now;

            var recipientHead = await worker.Orm.Select<HeadMessage>()
                .Where(e => e.UserId == recipientId && e.ContactId == senderId)
                .FirstAsync();

            if (recipientHead == null)
            {
                recipientHead = new HeadMessage
                {
                    Id = randomGenerator.Guid,
                    UserId = recipientId,
                    ContactId = senderId,
                    Content = senderMessage.Content,
                    CreateTime = createTime,
                    LastMessageTime = createTime
                };

                await worker.Orm.Insert(recipientHead).ExecuteAffrowsAsync();
            }
            else
            {
                recipientHead.Content = senderMessage.Content;
                recipientHead.LastMessageTime = createTime;
                await worker.Orm.Update<HeadMessage>().SetSource(recipientHead).ExecuteAffrowsAsync();
            }

            var recipientMessage = senderMessage.MapTo(new ChatMessage());
            recipientMessage.Id = 0;
            recipientMessage.UserId = recipientId;
            recipientMessage.ContactId = senderId;
            recipientMessage.IsRead = false;
            recipientMessage.CreateTime = createTime;
            recipientMessage.HeadMessageId = recipientHead.Id;
            recipientMessage.IsSelf = false;
            recipientMessage.FileName = senderMessage.FileName;
            recipientMessage.LocalSourcePath = string.Empty;

            recipientMessage.Id = await worker.Orm.Insert(recipientMessage).ExecuteIdentityAsync();

            worker.Commit();
            return recipientMessage;
        }
        catch (Exception ex)
        {
            worker.Rollback();
            Console.WriteLine($"离线消息持久化失败：{ex}");
            await logger.LogAsync($"离线消息持久化失败：{ex}", "聊天服务器");
            return null;
        }
    }

    private async Task<List<ChatMessage>> PersistGroupMessage(ChatMessage senderMessage)
    {
        using var worker = orm.CreateUnitOfWork();
        try
        {
            var senderId = senderMessage.UserId;
            var groupId = senderMessage.ContactId;
            var createTime = senderMessage.CreateTime ?? DateTime.Now;

            var userIds = await orm.Select<UserContact>()
                .Where(e => e.ContactId == groupId && e.IsGroup && e.UserId != senderId)
                .ToListAsync(e => e.UserId);
            var recipientMessages = new List<ChatMessage>();

            foreach (var userId in userIds)
            {
                var recipientHead = await worker.Orm.Select<HeadMessage>()
                    .Where(e => e.UserId == userId && e.ContactId == groupId)
                    .FirstAsync();

                if (recipientHead == null)
                {
                    recipientHead = new HeadMessage
                    {
                        Id = randomGenerator.Guid,
                        UserId = userId,
                        ContactId = groupId,
                        Content = senderMessage.Content,
                        CreateTime = createTime,
                        LastMessageTime = createTime
                    };

                    await worker.Orm.Insert(recipientHead).ExecuteAffrowsAsync();
                }
                else
                {
                    recipientHead.Content = senderMessage.Content;
                    recipientHead.LastMessageTime = createTime;
                    await worker.Orm.Update<HeadMessage>().SetSource(recipientHead).ExecuteAffrowsAsync();
                }

                var recipientMessage = senderMessage.MapTo(new ChatMessage());
                recipientMessage.Id = 0;
                recipientMessage.UserId = userId;
                recipientMessage.ContactId = groupId;
                recipientMessage.GroupMemberId = senderId;
                recipientMessage.IsRead = false;
                recipientMessage.CreateTime = createTime;
                recipientMessage.HeadMessageId = recipientHead.Id;
                recipientMessage.IsSelf = false;
                recipientMessage.FileName = senderMessage.FileName;
                recipientMessage.LocalSourcePath = string.Empty;

                recipientMessage.Id = await worker.Orm.Insert(recipientMessage).ExecuteIdentityAsync();
                recipientMessages.Add(recipientMessage);
            }

            worker.Commit();

            return recipientMessages;
        }
        catch (Exception ex)
        {
            worker.Rollback();
            Console.WriteLine($"离线消息持久化失败：{ex}");
            await logger.LogAsync($"离线消息持久化失败：{ex}", "聊天服务器");
            return [];
        }
    }

    private void CleanupSocket(int tempKey, Socket socket, string reason)
    {
        _temp.TryRemove(tempKey, out _);
        _lastHeartbeat.TryRemove(socket, out _);
        _clientReceiveTasks.TryRemove(socket, out _);

        var endpoint = socket.RemoteEndPoint?.ToString() ?? "unknown";

        foreach (var userSocket in _userSockets.ToArray())
        {
            if (ReferenceEquals(userSocket.Value, socket))
            {
                _userSockets.TryRemove(userSocket.Key, out _);
            }
        }
        
        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // Ignore shutdown errors for disconnected clients.
        }

        logger.Log($"Socket:{endpoint}, 已清理：{reason}", "聊天服务器");
        socket.Dispose();
    }

    public void Dispose()
    {
        if (_tokenSource.IsCancellationRequested)
        {
            return;
        }

        _tokenSource.Cancel();
        foreach (var entry in _temp.ToArray())
        {
            CleanupSocket(entry.Key, entry.Value, "服务关闭");
        }

        if (_acceptLoopTask != null)
        {
            try
            {
                _acceptLoopTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                // Expected during shutdown.
            }
        }

        if (_receiveLoopTask != null)
        {
            try
            {
                _receiveLoopTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                // Expected during shutdown.
            }
        }

        try
        {
            Task.WaitAll(_clientReceiveTasks.Values.ToArray(), TimeSpan.FromSeconds(2));
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
        {
            // Expected during shutdown.
        }

        _serverSocket.Dispose();
        _tokenSource.Dispose();
    }
}