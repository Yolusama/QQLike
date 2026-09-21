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
    ISourceHandler sourceHandler,
    IProjectLogger logger) : IChatMessageService
{
    public async Task<ResponseResult<bool>> UploadFile(IFormFile file,ChatMessageType type,long taskId, string tempFileName, long current, long total,long buffeSize)
    {
        using var worker = orm.CreateUnitOfWork();
        using var cts = new CancellationTokenSource();
        try
        {
            using var stream = file.OpenReadStream();
            var fileInfo = new  FileInfo(Path.Combine(fileConfig.FileRootPath,
                Path.Combine(sourceHandler.FileRootPath(type), tempFileName)));
            var bytes = await sourceHandler.HandleWriteChunk(tempFileName, type, stream, buffeSize, cts.Token);
            var finished = current + bytes == total;
            await worker.Orm.Update<FileTransmissionTask>()
                .SetIf(!finished,e => e.Current, current + bytes)
                .SetIf(finished, e => new FileTransmissionTask
                {
                    State = FileTransmissionState.Finished.GetValue(),
                    FinishTime = DateTime.Now,
                    Current = total
                })
                .Where(e => e.Id == taskId)
                .ExecuteAffrowsAsync(cts.Token);
            if(finished)
            {
                var finalFile = new FileInfo(Path.Combine(fileConfig.FileRootPath,
                    Path.Combine(sourceHandler.FileRootPath(type), file.FileName)));
                if(!finalFile.Exists)
                    finalFile.Create().Close();
                fileInfo.MoveTo(finalFile.FullName,true);
            }
            worker.Commit();
            return ResponseResult<bool>.OK(finished);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"上传文件出现异常：{e}","上传文件");
            await cts.CancelAsync();
            return ResponseResult.Fail(e.Message).Generic<bool>();
        }
    }

    public async Task<byte[]> GetMessageFileSource(string sourceName,ChatMessageType type)
    {
        var isValid = await orm.Select<FileTransmission>()
            .Where(f => f.FileName == sourceName && !f.IsReceiveSide && f.IsValid)
            .AnyAsync();
        if (!isValid)
            throw new Exception("文件已失效！");
        var fileName = Path.Combine(fileConfig.FileRootPath,
            Path.Combine(sourceHandler.FileRootPath(type), sourceName));
        var fileInfo = new  FileInfo(fileName);
        if (!fileInfo.Exists)
            throw new Exception("文件源已缺失");
        return await fileInfo.ReadBytes();
    }

    public async Task<ResponseResult<byte[]>> DownloadFile(ChatMessageType type, string fileName, long current, long total, long buffeSize)
    {
        using var cts = new  CancellationTokenSource();
        try
        {
            var isValid = await orm.Select<FileTransmission>()
                .Where(e => e.FileName == fileName && !e.IsReceiveSide)
                .ToOneAsync(e => e.IsValid,cts.Token);
            if(!isValid)
                return ResponseResult.Fail("文件已失效!").Generic<byte[]>();
            var bytes = await sourceHandler.HandReadChunk(fileName, current,type, buffeSize, cts.Token);
            return ResponseResult<byte[]>.OK(bytes);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await cts.CancelAsync();
            throw;
        }
     
    }

    public async Task<ResponseResult> RemoveTempFile(string tempFileName,ChatMessageType type)
    {
        if (string.IsNullOrEmpty(tempFileName))
            return ResponseResult.Fail("文件名不能为空");
        using var cts = new CancellationTokenSource();
        try
        {
            await sourceHandler.RemoveTempFile(tempFileName, type,cts.Token);
            return ResponseResult.OK();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await cts.CancelAsync();
            throw;
        }
        
    }
}