using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using QQLike.Entity.Configuration.Server;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Functional.Instructure;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class SocketServerService(SysSetting setting,IProjectLogger logger) : ISocketServerService
{
   private readonly ConcurrentDictionary<int, Socket> _temp = new();
   private readonly ConcurrentDictionary<string,Socket> _userSockets = new ();
   private readonly ConcurrentDictionary<string, HashSet<Socket>> _groupSockets = new ();
   private readonly ConcurrentDictionary<Socket, DateTime> _lastHeartbeat = new();
   
   private readonly Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
   private readonly CancellationTokenSource _tokenSource = new();
   private Task? _acceptLoopTask;
   private Task? _receiveLoopTask;
   private int _isStarted;
   private const int MaxQueueCount = 1000;
   private const int BufferSize = 4096;
   private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(30);
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
            var port = (socket.RemoteEndPoint as IPEndPoint)?.Port ?? socket.GetHashCode();
            _temp[port] = socket;
            _lastHeartbeat[socket] = DateTime.UtcNow;
            
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

   private async Task ReceiveThread(CancellationToken token)
   {
      var buffer = new byte[BufferSize];
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

            try
            {
               if (socket.Available <= 0)
               {
                  continue;
               }

               var receiveBytes = new List<byte>();
               while (socket.Available > 0)
               {
                  var bytesRead = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None, token);
                  if (bytesRead <= 0)
                  {
                     break;
                  }

                  receiveBytes.AddRange(buffer.Take(bytesRead));
               }

               if (receiveBytes.Count == 0)
               {
                  continue;
               }

               var model = JsonSerializer
                  .Deserialize<ChatMessageTransModel>(receiveBytes.ToArray());
               if (model is null)
               {
                  continue;
               }

               if (model.Type == ChatMessageType.Head)
               {
                  var userId = ParseDataAsString(model.Data);
                  if (!string.IsNullOrWhiteSpace(userId))
                  {
                     _userSockets[userId] = socket;
                  }
                  _lastHeartbeat[socket] = DateTime.UtcNow;
               }
               else if (model.Type == ChatMessageType.Heartbeat)
               {
                  _lastHeartbeat[socket] = DateTime.UtcNow;
                  var transModel = new ChatMessageTransModel
                  {
                     Type = ChatMessageType.Heartbeat,
                     Message = "收到心跳消息!"
                  };
                  await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(transModel))), SocketFlags.None, token);
               }
               else if (model.Type == ChatMessageType.Text)
               {
                  _lastHeartbeat[socket] = DateTime.UtcNow;
               }
               else
               {
                  _lastHeartbeat[socket] = DateTime.UtcNow;
               }
            }
            catch (OperationCanceledException)
            {
               break;
            }
            catch (SocketException)
            {
               CleanupSocket(tempKey, socket, "接收消息失败，已断开连接");
            }
            catch
            {
               CleanupSocket(tempKey, socket, "消息处理异常，已断开连接");
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

   private void CleanupSocket(int tempKey, Socket socket, string reason)
   {
      _temp.TryRemove(tempKey, out _);
      _lastHeartbeat.TryRemove(socket, out _);

      foreach (var userSocket in _userSockets.ToArray())
      {
         if (ReferenceEquals(userSocket.Value, socket))
         {
            _userSockets.TryRemove(userSocket.Key, out _);
         }
      }

      foreach (var group in _groupSockets.ToArray())
      {
         var sockets = group.Value;
         lock (sockets)
         {
            sockets.Remove(socket);
            if (sockets.Count == 0)
            {
               _groupSockets.TryRemove(group.Key, out _);
            }
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
      
      logger.Log($"Socket:{socket.RemoteEndPoint}, 已清理：{reason}", "聊天服务器");
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

      _serverSocket.Dispose();
      _tokenSource.Dispose();
   }
}