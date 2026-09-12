using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.DTO;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class UserChatSourceHandler(
    ISessionStorage sessionStorage,
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
            var directory = new DirectoryInfo(path);
            if(!directory.Exists)
                directory.Create();
            _baseDirectory = path;
            return path;
        }
    }

    private string DownloadPath(string fileName,ChatMessageType type)
    {
        var tempFileName = fileName.Substring(0, fileName.LastIndexOf('.')) + Constants.TempFileSuffix;
        var downloadPath = Path.Combine(UserBaseDirectory, FileRootPath(type));
        if(!Directory.Exists(downloadPath))
            Directory.CreateDirectory(downloadPath);
        var path = Path.Combine(downloadPath, tempFileName);
        if(!File.Exists(path))
            File.Create(path).Close();
        return path;
    }
    

    public async Task<string> Receive(FileTypeMessageModel model,CancellationToken token = default)
    {
        var filePath = Path.Combine(UserBaseDirectory, FileRootPath(model.Type));
        var directory = new DirectoryInfo(filePath);
        if(!directory.Exists)
            directory.Create();
        var toStoreName = Path.Combine(filePath, model.FileName);
        await using var newFileStream = new FileStream(toStoreName,
            FileMode.Create, FileAccess.ReadWrite,FileShare.ReadWrite);
        await newFileStream.WriteAsync(model.FileBytes,token);
        await newFileStream.FlushAsync(token);
        return toStoreName;
    }

    public async Task ReceivePart(FileTypeMessageModel model,bool finished, CancellationToken token = default)
    {
        var filePath = DownloadPath(model.FileName, model.Type);
        var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        try
        {
            await stream.WriteAsync(model.FileBytes, token);
            await stream.FlushAsync(token);
            stream.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            await stream.DisposeAsync();
        }

        if (finished)
        {
            var realFileName = model.FileName.Replace(filePath, Path.GetExtension(model.FileName));
            if(!File.Exists(realFileName))
                File.Create(realFileName).Close();
            File.Move(filePath, realFileName, true);
        }
    }

    public async Task RemoveTemp(FileTypeMessageModel model, CancellationToken token = default)
    {
        var fileRootPath = Path.Combine(UserBaseDirectory, FileRootPath(model.Type));
        var file =new FileInfo(Path.Combine(fileRootPath, model.FileName));
        await Task.Run(() =>
        {
            if(file.Exists)
                file.Delete();
        }, token);
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

    public string GetUrl(string sourceName, ChatMessageType type)
    {
        return type switch
        {
            ChatMessageType.Image => ImageUrl(sourceName),
            ChatMessageType.Video => VideoUrl(sourceName),
            ChatMessageType.Audio => AudioUrl(sourceName),
            ChatMessageType.File => CommonUrl(sourceName),
            _ => throw new Exception("非指向文件传输的消息类型")
        };
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