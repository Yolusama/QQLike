using Microsoft.AspNetCore.Mvc;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Services.Interfaces;

namespace QQLike.Controllers;

[Route("api/[controller]/[action]")]
public class VerificationMessageController(IVerificationMessageService verificationMessageService) : ControllerBase
{
    [HttpGet("{userId}")]
    public async Task<ActionResult<ResponseResult<List<VerificationMessageVO>>>> 
        GetVerificationMessages([FromRoute] string userId,bool? isGroup)
    {
        return await verificationMessageService.GetVerificationMessages(userId, isGroup);
    }
}