using QQLike.Entity.DTO;
using QQLike.Entity.Model;

namespace QQLike.Services.Interfaces;

public interface IUserService
{
   // public ResponseResult Login(string userAccount, string password);
   public Task<ResponseResult<string>> Register(UserRegisterModel user);
   public Task<ResponseResult<UserLoginDTO>> Login(UserLoginModel user);
   public Task<ResponseResult<List<long>>> GetUserContactGroups(string userId);
}