using QQLike.Entity.Common;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class UserChatSourceHandler(ISessionStorage sessionStorage) : IUserChatSourceHandler
{
    private const string StoreDirectory = "AppData";

    private string UserBaseDirectory
    {
        get
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            return Path.Combine(StoreDirectory, user.Account);
        }
    }
    
    public Task Store()
    {
        return Task.CompletedTask;
    }

    public Task Load()
    {
        return Task.CompletedTask;
    }
}