using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.DTO;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class UserChatSourceHandler(ISessionStorage sessionStorage,
    IRandomGenerator generator,
    FileConfig fileConfig,
    SysSetting setting) : IUserChatSourceHandler
{
    private string _baseDirectory = string.Empty;

    private string UserBaseDirectory
    {
        get
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            if(!string.IsNullOrEmpty(_baseDirectory))
                return _baseDirectory;
            var path = Path.Combine(setting.FileStorePath, user.Account);
            _baseDirectory = path;
            return path;
        }
    }
    

    public async Task<string> Receive(FileTypeMessageModel model,CancellationToken token)
    {
        var filePath = Path.Combine(UserBaseDirectory, FileRootPath(model.Type));
        var toStoreName = Path.Combine(filePath, model.FileName);
        await using var newFileStream = new FileStream(toStoreName,
            FileMode.Create, FileAccess.ReadWrite,FileShare.ReadWrite);
        await newFileStream.WriteAsync(model.FileBytes,token);
        await newFileStream.FlushAsync(token);
        return toStoreName;
    }

    public async Task Store(FileTypeMessageModel model,CancellationToken token)
    {
        var root = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), fileConfig.FileRootPath));
        var rootPath = Path.Combine(root.FullName, FileRootPath(model.Type));
        var toStore = Path.Combine(rootPath, model.FileName);
        await using var newFileStream = new FileStream(toStore,
            FileMode.Create, FileAccess.ReadWrite);
        await newFileStream.WriteAsync(model.FileBytes,token);
        await newFileStream.FlushAsync(token);
    }

    public string ImageUrl(string sourceName)
    {
        return $"{setting.ApiUrl}/Files/{fileConfig.ImagePath}/{sourceName}";
    }

    public string AudioUrl(string sourceName)
    {
        return $"{setting.ApiUrl}/Files/{fileConfig.AudioPath}/{sourceName}";
    }

    public string VideoUrl(string sourceName)
    {
        return $"{setting.ApiUrl}/Files/{fileConfig.VideoPath}/{sourceName}";
    }

    public string CommonUrl(string sourceName)
    {
        return $"{setting.ApiUrl}/Files/{fileConfig.CommonPath}/{sourceName}";
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