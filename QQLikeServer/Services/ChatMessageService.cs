using QQLike.Entity;
using QQLike.Entity.Configuration;
using QQLike.Entity.Enum;
using QQLike.Entity.Result;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class ChatMessageService(IFreeSql orm,
    FileConfig fileConfig,
    ISourceHandler sourceHandler) : IChatMessageService
{
    public Task<ResponseResult> UploadFile(IFormFile file, long messageId, string fileName, int current, int total)
    {
        return null;
    }

    public async Task<byte[]> GetMessageFileSource(string sourceName,long messageId, ChatMessageType type)
    {
        var isValid = await orm.Select<FileTransmission>()
            .Where(f => f.MessageId == messageId)
            .FirstAsync(f => f.IsValid);
        if (!isValid)
            throw new Exception("文件已失效！");
        var fileName = Path.Combine(fileConfig.FileRootPath,
            Path.Combine(sourceHandler.FileRootPath(type), sourceName));
        var fileInfo = new  FileInfo(fileName);
        if (!fileInfo.Exists)
            throw new Exception("文件源已缺失");
        return await fileInfo.ReadBytes();

    }
}