using QQLike.Entity;
using QQLike.Entity.Configuration;
using QQLike.Entity.Result;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class ChatMessageService(IFreeSql orm, FileConfig fileConfig) : IChatMessageService
{
    public Task<ResponseResult> UploadFile(IFormFile file, long messageId, string fileName, int current, int total)
    {
        return null;
    }
}