using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Services.Interfaces;

namespace QQLike.Controllers;

[Route("api/[controller]/[action]")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ResponseResult<UserLoginVO>>> Login([FromBody] UserLoginModel model)
    {
        return await userService.Login(model);
    }
    
    [HttpPut]
    public async Task<ActionResult<ResponseResult<string>>> Register([FromBody] UserRegisterModel model)
    {
        return await userService.Register(model);
    }
    
}