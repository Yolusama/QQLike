using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QQLike.Entity.Common;
using QQLike.Entity.DTO;
using QQLike.Entity.VO;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class JwtAuthorizeFilter(IJwtService jwtService,IRedisCache redis) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var needAuthorize = context.ActionDescriptor.EndpointMetadata
            .OfType<AuthorizeAttribute>().Any();

        if (!needAuthorize)
        {
            return; // 允许匿名，跳过
        }

        var headers =  context.HttpContext.Request.Headers;
        var token = headers["Authorization"].ToString().Split(' ').Last();
        var userTokenInfo = jwtService.Parse<UserTokenInfo>(token);
        var key = $"{userTokenInfo.UserId}_{CachingKeys.UserToken}";
        if(!redis.Exists(key))
        {
            context.Result = new JsonResult(new ResponseResult<string>
            {
                Code = 401,
                Message = "用户凭证已过期，请重新登录",
                Data = null
            });
            return;
        }
        var cacheToken = 
            redis.Get<string>($"{userTokenInfo.UserId}_{CachingKeys.UserToken}");
        if (string.IsNullOrEmpty(cacheToken) || cacheToken != token)
        {
            context.Result = new JsonResult(new ResponseResult<string>
            {
                Code = 401,
                Message = "用户凭证不正确",
                Data = null
            });
            return; 
        }
    }
}