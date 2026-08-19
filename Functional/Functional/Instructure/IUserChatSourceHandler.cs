namespace QQLike.Functional.Instructure;

public interface IUserChatSourceHandler
{
    public Task Store();
    public Task Load();
}