using QQLike.Entity;
using QQLike.Entity.Enum;
using QQLike.Entity.Result;

namespace QQLike.Services.Interfaces;

public interface IChatMessageService
{
   public Task<ResponseResult> UploadFile(IFormFile file,long messageId,string fileName,int current,int total);
   public Task<byte[]> GetMessageFileSource(string sourceName,long messageId,ChatMessageType type);
}