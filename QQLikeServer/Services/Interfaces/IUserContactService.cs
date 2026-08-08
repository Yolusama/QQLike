using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;

namespace QQLike.Services.Interfaces;

public interface IUserContactService
{
    public Task<ResponseResult<List<long>>> GetUserContactGroups(string userId);
    public Task<ResponseResult<List<UserContactGroupingVO>>> GetUserContactGrouping(string userId);
    public Task<ResponseResult<long>> AddUserContactGroup(UserContactGroupModel model);
    public  Task<ResponseResult<List<UserContactManageVO>>> GetUserManageFriends(string userId,long userContactGroupId = 0);
    //public Task<ResponseResult<long>> AddUserContact(UserContactModel model);
}