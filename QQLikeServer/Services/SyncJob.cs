using QQLike.Entity;
using QQLike.Entity.Configuration;
using QQLike.Functional.Instructure;
using QQLike.Services.Interfaces;
using SysSetting = QQLike.Entity.Configuration.Server.SysSetting;

namespace QQLike.Services;

public class SyncJob(
    IFreeSql orm,
    IProjectLogger logger,
    FileConfig fileConfig) : ISyncJob
{
    public async Task RemoveStoredFile()
    {
        using var worker = orm.CreateDbContext();
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
             var commonRemoveTask = Handle(commonFiles,token);
             var tempDirectory = new DirectoryInfo(Path.Combine(fileConfig.FileRootPath, fileConfig.TempPath));
             var  tempFiles = tempDirectory.GetFiles();
             var tempRemoveTask = Handle(tempFiles,token,true);
             
             await Task.WhenAll(imagesRemoveTask,audioRemoveTask,videoRemoveTask,commonRemoveTask,tempRemoveTask)
                 .ConfigureAwait(false);
        }
        catch (Exception e)
        {
             await logger.LogAsync($"清理文件时发生异常: {e}","聊天缓存文件清理");
             await cts.CancelAsync();
        }
    }

    private Task Handle(IEnumerable<FileInfo> files,CancellationToken token,bool removeTemp = false)
    {
        var toRemove = new List<(long,string)>();
        var task = new Task(() =>
        {
            foreach (var file in files)
            {
                var fileName = file.Name;
                var transmission = orm.Select<FileTransmission>()
                    .Where(e => e.FileName == fileName && e.IsValid)
                    .First(e=>new {e.Id,e.CreateTime});
                var now = DateTime.Now;
                var validTime = removeTemp ? transmission.CreateTime.Value.AddDays(fileConfig.TempFileExpireDays) 
                    : transmission.CreateTime.Value.AddDays(fileConfig.FileExpireDays);
                if(validTime >= now.AddSeconds(-now.Second))
                {
                    file.Delete();
                    toRemove.Add((transmission.Id,file.FullName));
                }
               
            }
            if(toRemove.Count > 0)
            {
                var toRemoveIds = toRemove.Select(e => e.Item1).ToList();
                orm.Update<FileTransmission>()
                    .Set(e => e.IsValid, false)
                    .Where(e => toRemoveIds.Contains(e.Id))
                    .ExecuteAffrows();
                logger.Log($"清理文件完成，共清理{toRemove.Count}个文件,文件：\r\n{string.Join("\r\n", toRemove.Select(e => e.Item2))}","聊天缓存文件清理");
            }
        },token);
        task.Start();
        return task;
    }
}