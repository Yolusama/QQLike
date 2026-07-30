using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QQLike.Entity.DTO;
using QQLike.Entity.Model;
using QQLike.Services.Interfaces;

namespace QQLike.Controllers;

[Route("api/[controller]/[action]")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ResponseResult<UserLoginDTO>>> Login([FromBody] UserLoginModel model)
    {
        return await userService.Login(model);
    }
    
    [HttpPut]
    public async Task<ActionResult<ResponseResult<string>>> Register([FromBody] UserRegisterModel model)
    {
        return await userService.Register(model);
    }
    
    [Authorize]
    [HttpGet("{userId}")]
    public async Task<ActionResult<ResponseResult<List<long>>>> ContactGroups([FromRoute] string userId)
    {
        return await userService.GetUserContactGroups(userId);
    }
}