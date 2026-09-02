
using QQLike.Entity.Enum;
using QQLike.Entity.Model;

namespace QQLike.Functional.Instructure;

public interface IUserChatSourceHandler
{
    public Task<string> Receive(FileTypeMessageModel model,CancellationToken token);
    public Task Store(FileTypeMessageModel model,CancellationToken token);
    public string ImageUrl(string sourceName);
    public string  FileRootPath(ChatMessageType type);
    public string AudioUrl(string sourceName);
    public string VideoUrl(string sourceName);
    public string CommonUrl(string sourceName);
    
}