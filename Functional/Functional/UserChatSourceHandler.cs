using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.DTO;
using QQLike.Entity.Model;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class UserChatSourceHandler(ISessionStorage sessionStorage,
    IRandomGenerator generator,
    SysSetting setting) : IUserChatSourceHandler
{
    private const string StoreDirectory = "AppData";
    private string _baseDirectory = string.Empty;

    private string UserBaseDirectory
    {
        get
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            if(!string.IsNullOrEmpty(_baseDirectory))
                return _baseDirectory;
            var path = Path.Combine(StoreDirectory, user.Account);
            _baseDirectory = path;
            return path;
        }
    }
    
    /// <summary>
    /// 截图其发送实现基础
    /// </summary>
    /// <param name="file"></param>
    public async Task ScreenShotStore(FileInfo file)
    {
        await using var fileStream = new FileStream(file.FullName, FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite); 
        var fileName =generator.Guid + file.Extension;
        await using var newFileStream = new FileStream(Path.Combine(UserBaseDirectory, fileName),
            FileMode.Create, FileAccess.ReadWrite);
        await fileStream.CopyToAsync(newFileStream);
        await newFileStream.FlushAsync();
    }

    public async Task Receive(FileTypeMessageModel model)
    {
        await using var newFileStream = new FileStream(Path.Combine(UserBaseDirectory, model.FileName),
            FileMode.Create, FileAccess.ReadWrite);
        await newFileStream.WriteAsync(model.FileBytes, 0, 
            model.FileBytes.Length);
        await newFileStream.FlushAsync();
    }

    public string ImageUrl(string sourceName)
    {
        return $"{setting.ApiUrl}/Files/Images/{sourceName}";
    }
}