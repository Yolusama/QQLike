using Hangfire;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class JobRunner : IJobRunner
{
    public void Run()
    {
        RecurringJob.AddOrUpdate<ISyncJob>(a=>a.RemoveStoredFile(),Cron.Minutely(),
            TimeZoneInfo.Local);
    }
}