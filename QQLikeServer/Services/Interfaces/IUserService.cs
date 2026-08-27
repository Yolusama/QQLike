using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;

namespace QQLike.Services.Interfaces;

public interface IUserService
{
   // public ResponseResult Login(string userAccount, string password);
   public Task<ResponseResult<string>> Register(UserRegisterModel user);
   public Task<ResponseResult<UserLoginVO>> Login(UserLoginModel user);
   public Task<ResponseResult<UserVerifyInfo>> GetUserVerifyInfo(string account);
   public Task<ResponseResult<UserContactCardInfo>> GetUserContactCardInfo(string userId,string contactId);
   public Task<ResponseResult<List<UserContactInfo>>> GetUserContactInfo(string userId,string search,bool withGroup);
}