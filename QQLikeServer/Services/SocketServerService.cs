using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using QQLike.Entity.Configuration.Server;
using QQLike.Functional.Instructure;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class SocketServerService(SysSetting setting,IProjectLogger logger) : ISocketServerService
{
   private readonly ConcurrentDictionary<int,Socket> _sockets = new ();
   private readonly ConcurrentDictionary<int, HashSet<Socket>> _groupSockets = new ();
   
   private readonly Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
   private readonly CancellationTokenSource _tokenSource = new();
   private Task? _acceptLoopTask;
   private int _isStarted;
   private const int MaxQueueCount = 1000;

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
   }

   private async Task SocketThread(CancellationToken token)
   {
      while (!token.IsCancellationRequested)
      {
         try
         {
            var socket = await _serverSocket.AcceptAsync(token);
            var ip = socket.RemoteEndPoint as IPEndPoint;
            _sockets.TryAdd(ip.Port, socket);
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

   public void Dispose()
   {
      if (_tokenSource.IsCancellationRequested)
      {
         return;
      }

      _tokenSource.Cancel();
      foreach (var socket in _sockets.Values)
      {
         try
         {
            socket.Shutdown(SocketShutdown.Both);
         }
         catch
         {
            // Ignore shutdown errors for disconnected clients.
         }

         socket.Dispose();
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
      _serverSocket.Dispose();
      _tokenSource.Dispose();
   }
}