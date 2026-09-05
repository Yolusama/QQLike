using QQLike.Entity.Configuration;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class SourceHandler(FileConfig fileConfig) : ISourceHandler
{
    public async Task<string> Store(FileTypeMessageModel model,CancellationToken token)
    {
        var root = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), fileConfig.FileRootPath));
        var rootPath = Path.Combine(root.FullName, FileRootPath(model.Type));
        var toStore = Path.Combine(rootPath, model.FileName);
        await using var newFileStream = new FileStream(toStore,
            FileMode.Create, FileAccess.ReadWrite);
        await newFileStream.WriteAsync(model.FileBytes,token);
        await newFileStream.FlushAsync(token);
        return toStore;
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