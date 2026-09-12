
using QQLike.Entity.DTO;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;

namespace QQLike.Functional.Instructure;

public interface IUserChatSourceHandler
{
    public Task<string> Receive(FileTypeMessageModel model,CancellationToken token = default);
    public Task ReceivePart(FileTypeMessageModel model,bool finished,CancellationToken token = default);
    public Task RemoveTemp(FileTypeMessageModel model,CancellationToken token = default);
    public string GetUrl(string sourceName, ChatMessageType type);
    public string ImageUrl(string sourceName);
    public string  FileRootPath(ChatMessageType type);
    public string AudioUrl(string sourceName);
    public string VideoUrl(string sourceName);
    public string CommonUrl(string sourceName);
    
}