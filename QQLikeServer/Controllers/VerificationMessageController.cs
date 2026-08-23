using Microsoft.AspNetCore.Mvc;
using QQLike.Entity.Attributes;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Services.Interfaces;

namespace QQLike.Controllers;

[Route("api/[controller]/[action]")]
public class VerificationMessageController(IVerificationMessageService verificationMessageService) : ControllerBase
{
    [HttpGet("{userId}")]
    [RequestAuthorize]
    public async Task<ActionResult<ResponseResult<List<VerificationMessageVO>>>> 
        GetVerificationMessages([FromRoute] string userId,bool? isGroup)
    {
        return await verificationMessageService.GetVerificationMessages(userId, isGroup);
    }
    
    [HttpPost]
    [RequestAuthorize]
    public async Task<ActionResult<ResponseResult>> AddVerificationMessage([FromBody]VerificationMessageModel model)
    {
        return await verificationMessageService.AddVerificationMessage(model);
    }
}