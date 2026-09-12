using QQLike.Entity.Configuration;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class SourceHandler(FileConfig fileConfig) : ISourceHandler
{
    public async Task<string> Store(FileTypeMessageModel model,CancellationToken token)
    {
        var rootPath = Path.Combine(fileConfig.FileRootPath, FileRootPath(model.Type));
        var toStore = Path.Combine(rootPath, model.FileName);
        await using var newFileStream = new FileStream(toStore,
            FileMode.Create, FileAccess.ReadWrite);
        await newFileStream.WriteAsync(model.FileBytes,token);
        await newFileStream.FlushAsync(token);
        return toStore;
    }

    public async Task HandleWriteChunk(string fileName,ChatMessageType type, Stream chunkStram, long buffeSize, CancellationToken token)
    {
        var tempRoot = Path.Combine(fileConfig.FileRootPath, FileRootPath(type));
        using var stream = new FileStream(Path.Combine(tempRoot, fileName), FileMode.Append,FileAccess.Write, FileShare.ReadWrite);
        var buffer = new byte[buffeSize];
        var bytesRead = await chunkStram.ReadAsync(buffer,token);
        await stream.WriteAsync(buffer.Take(bytesRead).ToArray(),token);
    }

    public async Task<byte[]> HandReadChunk(string fileName, int current, int total, ChatMessageType type, long bufferSize,
        CancellationToken token)
    {
        var tempRoot = Path.Combine(fileConfig.FileRootPath, FileRootPath(type));
        var buffer = new byte[bufferSize];
        using var stream = new FileStream(Path.Combine(tempRoot, fileName), FileMode.Open,FileAccess.Read, FileShare.ReadWrite);
        stream.Seek((current - 1) * bufferSize,SeekOrigin.Begin);
        var bytesRead =await stream.ReadAsync(buffer, token);
        return buffer.Take(bytesRead).ToArray();
    }

    public string FileRootPath(ChatMessageType type)
    {
        return type switch 
        {
            ChatMessageType.Image => fileConfig.ImagePath,
            ChatMessageType.Video => fileConfig.VideoPath,
            ChatMessageType.Audio => fileConfig.AudioPath,
            ChatMessageType.File => fileConfig.CommonPath,
            _ => throw new  Exception("非指向文件传输的消息类型")
        };
    }
}