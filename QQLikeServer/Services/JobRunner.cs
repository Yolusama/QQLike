using Hangfire;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class JobRunner : IJobRunner
{
    public void Run()
    {
        RecurringJob.AddOrUpdate<ISyncJob>("清理传输文件",a=>a.RemoveStoredFile(),Cron.Minutely(),
            TimeZoneInfo.Local);
        RecurringJob.AddOrUpdate<ISyncJob>("清理传输临时文件",a=>a.ClearTemp(),Cron.Minutely(),
            TimeZoneInfo.Local);
    }
}