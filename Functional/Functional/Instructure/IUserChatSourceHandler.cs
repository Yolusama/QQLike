
using QQLike.Entity.Model;

namespace QQLike.Functional.Instructure;

public interface IUserChatSourceHandler
{
    public Task ScreenShotStore(FileInfo file);
    public Task Receive(FileTypeMessageModel model);
    public string ImageUrl(string sourceName);
}