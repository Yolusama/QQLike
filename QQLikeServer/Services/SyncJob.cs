using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class SyncJob(
    IFreeSql orm,
    IProjectLogger logger,
    FileConfig fileConfig) : ISyncJob
{
    public async Task RemoveStoredFile()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        try
        {
             var imagesDirectory = new DirectoryInfo(Path.Combine(fileConfig.FileRootPath, fileConfig.ImagePath));
             var images = imagesDirectory.GetFiles().Where(f => !f.Name.Contains("default")).ToList();
             var imagesRemoveTask = Handle(images, token);
             var audioDirectory = new DirectoryInfo(Path.Combine(fileConfig.FileRootPath, fileConfig.AudioPath));
             var audios = audioDirectory.GetFiles();
             var audioRemoveTask = Handle(audios,token);
             var videoDirectory = new DirectoryInfo(Path.Combine(fileConfig.FileRootPath, fileConfig.VideoPath));
             var videos = videoDirectory.GetFiles();
             var videoRemoveTask = Handle(videos,token);
             var commonDirectory = new DirectoryInfo(Path.Combine(fileConfig.FileRootPath, fileConfig.CommonPath));
             var commonFiles = commonDirectory.GetFiles();
             var commonRemoveTask = Handle(commonFiles,token); ;
             
             await Task.WhenAll(imagesRemoveTask,audioRemoveTask,videoRemoveTask,commonRemoveTask)
                 .ConfigureAwait(false);
        }
        catch (Exception e)
        {
             await logger.LogAsync($"清理文件时发生异常: {e}","聊天缓存文件清理");
             await cts.CancelAsync();
        }
    }

    public async Task ClearTemp()
    {
        var worker = orm.CreateUnitOfWork();
        var cts = new CancellationTokenSource();
        try
        {
            var tasks = await orm.Select<FileTransmissionTask, FileTransmission>()
                .LeftJoin(e => e.t1.Id == e.t2.TaskId)
                .Where(e => !e.t2.IsValid && !e.t2.IsReceiveSide)
                .ToListAsync(e => new { e.t1.Id, e.t1.TempFileName },cts.Token);

            if (tasks.Count > 0)
            {
                var taskIds = tasks.Select(e => e.Id).ToList();
                await worker.Orm.Delete<FileTransmissionTask>()
                    .Where(e => taskIds.Contains(e.Id))
                    .ExecuteAffrowsAsync(cts.Token);
                await logger.LogAsync(
                    $"清理无效的临时文件任务完成，共清理{tasks.Count}个任务,文件：\r\n{string.Join("\r\n", tasks.Select(f => f.TempFileName))}",
                    "聊天缓存文件清理");
            }

            worker.Commit();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            worker.Rollback();
            await logger.LogAsync($"清理无效的临时文件任务时发生异常: {e}", "聊天缓存文件清理");
            await cts.CancelAsync();
        }
        finally
        {
            worker.Dispose();
            cts.Dispose();
        }
    }

    private Task Handle(IEnumerable<FileInfo> files,CancellationToken token)
    {
        var toRemove = new List<FileClearVO>();
        var task = new Task(() =>
        {
            using var worker = orm.CreateUnitOfWork();
            try
            {
                foreach (var file in files)
                {
                    var fileName = file.Name;
                    var isTemp = fileName.EndsWith(Constants.TempFileSuffix);
                    var tempFileName = isTemp ? fileName.Replace(Constants.TempFileSuffix,file.Extension)
                        : null;
                    var transmission = orm.Select<FileTransmission>()
                        .Where(e=> e.IsValid && !e.IsReceiveSide)
                        .WhereIf(!isTemp,e => e.FileName == fileName)
                        .WhereIf(isTemp,e=>e.FileName == tempFileName)
                        .First(e=>new {e.Id,e.CreateTime});
                    if(transmission == null) continue;
                    var now = DateTime.Now;
                    var validTime = isTemp ? transmission.CreateTime.Value.AddDays(fileConfig.TempFileExpireDays) 
                        : transmission.CreateTime.Value.AddDays(fileConfig.FileExpireDays);
                    if(validTime <= now.AddSeconds(-now.Second))
                    {
                        file.Delete();
                        toRemove.Add(new FileClearVO
                        {
                            TransmissionId = transmission.Id,
                            File = file,
                            RootDirectory = file.Directory
                        });
                    }
               
                }
                if(toRemove.Count > 0)
                {
                    var toRemoveIds = toRemove.Select(e => e.TransmissionId).ToList();
                    worker.Orm.Update<FileTransmission>()
                        .Set(e => e.IsValid, false)
                        .Where(e => toRemoveIds.Contains(e.Id))
                        .ExecuteAffrows();
                    logger.Log($"清理文件完成，共清理{toRemove.Count}个文件,文件：\r\n{string.Join("\r\n", toRemove.Select(f=>f.File.Name))}","聊天缓存文件清理");
                }
                
                worker.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                worker.Rollback();
                logger.Log($"清理文件时发生异常: {e}","聊天缓存文件清理");
            }
        },token);
        task.Start();
        return task;
    }
}