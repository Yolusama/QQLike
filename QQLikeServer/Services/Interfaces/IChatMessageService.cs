using QQLike.Entity;
using QQLike.Entity.Enum;
using QQLike.Entity.Result;

namespace QQLike.Services.Interfaces;

public interface IChatMessageService
{
   public Task<ResponseResult<bool>> UploadFile(IFormFile file,ChatMessageType type,long taskId,string tempFileName,int current,int total,long buffeSize);
   public Task<byte[]> GetMessageFileSource(string sourceName,ChatMessageType type);
   public Task<ResponseResult<byte[]>> DownloadFile(ChatMessageType type,long taskId,string fileName,int current,int total,long buffeSize);
   public Task<ResponseResult> RemoveTempFile(string tempFileName,ChatMessageType type);
}