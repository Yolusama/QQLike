using QQLike.Entity;
using QQLike.Entity.Enum;
using QQLike.Entity.Result;

namespace QQLike.Services.Interfaces;

public interface IChatMessageService
{
   public Task<ResponseResult<bool>> UploadFile(IFormFile file,ChatMessageType type,long taskId,string tempFileName,long current,long total,long buffeSize);
   public Task<byte[]> GetMessageFileSource(string sourceName,ChatMessageType type);
   public Task<ResponseResult<byte[]>> DownloadFile(ChatMessageType type,string fileName,long current,long total,long buffeSize);
   public Task<ResponseResult> RemoveTempFile(string tempFileName,ChatMessageType type);
}