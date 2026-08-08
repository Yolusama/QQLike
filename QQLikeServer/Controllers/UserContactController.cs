using Microsoft.AspNetCore.Mvc;
using QQLike.Entity.Attributes;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Services.Interfaces;

namespace QQLike.Controllers;

[Route("api/[Controller]/[action]")]
public class UserContactController(IUserContactService userContactService) : ControllerBase
{
    [RequestAuthorize]
    [HttpGet("{userId}")]
    public async Task<ActionResult<ResponseResult<List<long>>>> ContactGroups([FromRoute] string userId)
    {
        return await userContactService.GetUserContactGroups(userId);
    }

    [RequestAuthorize]
    [HttpGet("{userId}")]
    public async Task<ActionResult<ResponseResult<List<UserContactGroupingVO>>>>
        GetUserContactGrouping([FromRoute] string userId)
    {
        return await userContactService.GetUserContactGrouping(userId);
    }
    
    [RequestAuthorize]
    [HttpPost]
    public async Task<ActionResult<ResponseResult<long>>> AddUserContactGroup([FromBody] UserContactGroupModel model)
    {
        return await userContactService.AddUserContactGroup(model);
    }

    [RequestAuthorize]
    [HttpGet("{userId}/{userContactGroupId}")]
    public async Task<ActionResult<ResponseResult<List<UserContactManageVO>>>> RemoveUserContactGroup(
        [FromRoute] string userId,[FromRoute] long userContactGroupId = 0)
    {
        return await userContactService.GetUserManageFriends(userId,userContactGroupId);
    }
}