using QQLike.Entity.Enum;
using QQLike.Entity.Model;

namespace QQLike.Functional.Instructure;

public interface ISourceHandler
{
    public Task<string> Store(FileTypeMessageModel model,CancellationToken token);
    public string  FileRootPath(ChatMessageType type);
}