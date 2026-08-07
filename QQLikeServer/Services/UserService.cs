using QQLike.Functional.Utils;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class UserService(IFreeSql orm,
    IRandomGenerator generator,
    IRedisCache redis,
    IJwtService jwtService) : IUserService
{
    public async Task<ResponseResult<string>> Register(UserRegisterModel model)
    {
        using var worker = orm.CreateUnitOfWork();
        try
        {
            if(!await redis.ExistsAsync($"{CachingKeys.VerificationCode}_{model.Email}"))
                return ResponseResult.Fail("验证码已过期，请重新获取").Generic<string>();
            var verificationCode = await redis.GetAsync<string>($"{CachingKeys.VerificationCode}_{model.Email}");
            if (verificationCode != model.VerificationCode)
                return ResponseResult.Fail("验证码错误").Generic<string>();
            var accountLength = Random.Shared.Next(10,15);
            var account = generator.GenerateByNumbers(accountLength);
            var data = model.MapTo(new User());
            data.Avatar = "default.png";
            data.CreateTime = DateTime.Now;
            data.Account = account;
            data.Id = generator.Guid;
            await worker.Orm.Insert(data).ExecuteAffrowsAsync();
            await redis.RemoveAsync($"{CachingKeys.VerificationCode}_{model.Email}");
            var userContactGroup = new UserContactGroup
            {
                UserId = data.Id,
                Name = "我的好友",
                CreateTime = DateTime.Now,
                IsGroup = false
            };
            var groupContact = new UserContactGroup
            {
                UserId = data.Id,
                Name = "加入的群聊",
                CreateTime = DateTime.Now,
                IsGroup = true
            };
            await worker.Orm.Insert(new List<UserContactGroup> { userContactGroup, groupContact })
                .ExecuteAffrowsAsync();
          
            worker.Commit();
            return ResponseResult<string>.OK("注册成功",account);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            worker.Rollback();
            return ResponseResult.Fail($"注册过程中程序出现异常:{e.Message}").Generic<string>();
        }
    }

    public async Task<ResponseResult<UserLoginVO>> Login(UserLoginModel  model)
    {
        using var worker = orm.CreateUnitOfWork();
        try
        {
            var user = await worker.Orm.Select<User>()
                .Where(u => u.Account == model.UserAccount)
                .FirstAsync();
            if(user == null)
                return ResponseResult.Fail("用户不存在").Generic<UserLoginVO>();
            if(user.Password != model.Password)
                return ResponseResult.Fail("密码错误").Generic<UserLoginVO>();
            var token = jwtService.Generate(new UserTokenInfo{ UserId = user.Id }, Constants.TokenExpire);
            await redis.SetAsync($"{user.Id}_{CachingKeys.UserToken}", token, Constants.TokenExpire);
            user.LastLoginTime = DateTime.Now;
            user.IsOnline = true;
            var userDTO = user.MapTo(new UserLoginVO());
            userDTO.Token = token;
            userDTO.UserId = user.Id;
            await worker.Orm.Update<User>().SetSource(user).ExecuteAffrowsAsync();
            worker.Commit();
            return ResponseResult<UserLoginVO>.OK("登录成功", userDTO);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            worker.Rollback();
            return ResponseResult.Fail($"登录过程中程序出现异常:{e.Message}").Generic<UserLoginVO>();
        }
    }

    public async Task<ResponseResult<UserVerifyInfo>> GetUserVerifyInfo(string account)
    {
        var user = await orm.Select<User>()
            .Where(u => u.Account == account)
            .FirstAsync();
        if(user == null)
            return ResponseResult.Fail("用户不存在").Generic<UserVerifyInfo>();
        var res = new UserVerifyInfo();
        res.Avatar = user.Avatar;
        res.Nickname = user.Nickname;
        res.UserId = user.Id;
        var contactGroups = await orm.Select<UserContactGroup>()
            .Where(c => c.UserId == user.Id && !c.IsGroup)
            .ToListAsync();
        res.ContactGroups = contactGroups;
        return ResponseResult<UserVerifyInfo>.OK("获取用户验证信息成功", res);
    }
}