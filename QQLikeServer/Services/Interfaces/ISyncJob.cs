namespace QQLike.Services.Interfaces;

public interface ISyncJob
{
    public Task RemoveStoredFile();
    public Task ClearTemp();
}