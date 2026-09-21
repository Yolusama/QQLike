using QQLike.Entity.Enum;
using QQLike.Entity.Model;

namespace QQLike.Functional.Instructure;

public interface ISourceHandler
{
    public Task<string> Store(FileTypeMessageModel model,CancellationToken token);
    public Task<int> HandleWriteChunk(string fileName,ChatMessageType type, Stream chunkStream,long bufferSize,CancellationToken token);
    public Task<byte[]> HandReadChunk(string fileName,long current,ChatMessageType type,long bufferSize,CancellationToken token);
    public Task RemoveTempFile(string tempFileName,ChatMessageType type,CancellationToken token = default);
    public string  FileRootPath(ChatMessageType type);
}