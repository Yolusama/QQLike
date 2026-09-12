using QQLike.Entity.Enum;
using QQLike.Entity.Model;

namespace QQLike.Functional.Instructure;

public interface ISourceHandler
{
    public Task<string> Store(FileTypeMessageModel model,CancellationToken token);
    public Task HandleWriteChunk(string fileName,ChatMessageType type, Stream chunkStream,long bufferSize,CancellationToken token);
    public Task<byte[]> HandReadChunk(string fileName,int current,int total,ChatMessageType type,long bufferSize,CancellationToken token);
    public string  FileRootPath(ChatMessageType type);
}