using Microsoft.AspNetCore.Mvc;
using QQLike.Entity.Attributes;
using QQLike.Entity.DTO;
using QQLike.Entity.Result;
using QQLike.Services.Interfaces;

namespace QQLike.Controllers;

[Route("api/[controller]/[action]")]
public class ChatGroupController(IChatGroupService chatGroupService) : ControllerBase
{
    [HttpPost]
    [RequestAuthorize]
    public async Task<ActionResult<ResponseResult<string>>> CreateChatGroup([FromBody] CreateChatGroupDTO dto)
    {
        return await chatGroupService.CreateChatGroup(dto);
    }
}