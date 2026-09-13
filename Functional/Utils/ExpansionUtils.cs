using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using QQLike.Entity;
using QQLike.Entity.Common;

namespace QQLike.Functional.Utils;

public static class ExpansionUtils
{
    private static readonly ConcurrentDictionary<Socket, SemaphoreSlim> SocketSendLocks = new();

    public static string ToSha256Str(this string str)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(str);
        var hashBytes = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "");
    }

    public static int GetValue(this Enum enumParam)
    {
        var obj = (object)enumParam;
        return (int)obj;
    }

    public static T2 MapTo<T1, T2>(this T1 src, T2 dest)
    {
        // Map properties from src to instance
        var destType = typeof(T2);
        var properties = typeof(T1).GetProperties();
        foreach (var property in properties)
        {
            var destProperty = destType.GetProperty(property.Name);
            if (destProperty != null && destProperty.PropertyType.IsAssignableFrom(property.PropertyType))
            {
                var value = property.GetValue(src);
                destProperty.SetValue(dest, value);
            }
        }

        return dest;
    }

    public static T2 MapTo<T1, T2>(this T1 src)
    {
        // Map properties from src to instance
        var dest = Activator.CreateInstance<T2>();
        return src.MapTo(dest);
    }

    public static string ToNormalJson<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj);
    }

    public static async Task<byte[]> ReadBytes(this FileInfo fileInfo)
    {
        int bufferSize = FileTransmission.GetBufferSize(fileInfo.Length);
        var buffer = new byte[bufferSize];
        var bytes = new List<byte>();
        using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        int bytesRead;
        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            bytes.AddRange(buffer.Take(bytesRead));
        return bytes.ToArray();
    }

    /// <summary>
    /// 先发送数据长度报文头再发送整体数据
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="bytes"></param>
    public static async Task SendWith(this Socket socket, byte[] bytes, CancellationToken token = default)
    {
        var sendLock = SocketSendLocks.GetOrAdd(socket, _ => new SemaphoreSlim(1, 1));
        await sendLock.WaitAsync(token);
        try
        {
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            await SendAllAsync(socket, lengthBytes, token);
            await SendAllAsync(socket, bytes, token);
        }
        finally
        {
            sendLock.Release();
        }
    }

    public static void SafeDispose(this IDisposable disposable)
    {
        try
        {
            disposable.Dispose();
        }
        catch { }
    }

    private static async Task SendAllAsync(Socket socket, byte[] buffer, CancellationToken token)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var sent = await socket.SendAsync(new ArraySegment<byte>(buffer, offset, buffer.Length - offset),
                SocketFlags.None, token);
            if (sent <= 0)
                throw new SocketException((int)SocketError.ConnectionReset);

            offset += sent;
        }
    }
}