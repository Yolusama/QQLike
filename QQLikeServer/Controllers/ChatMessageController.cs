using Microsoft.AspNetCore.Mvc;
using QQLike.Entity.Attributes;
using QQLike.Entity.Common;
using QQLike.Entity.Enum;
using QQLike.Services.Interfaces;

namespace QQLike.Controllers;

[Route("api/[controller]/[action]")]
public class ChatMessageController(IChatMessageService chatMessageService) : ControllerBase
{
    [RequestAuthorize]
    [HttpGet]
    public async Task<ActionResult> GetMessageFileSource([FromQuery]string fileName,
        [FromQuery]long messageId,
        [FromQuery]int type)
    {
        var bytes = await chatMessageService.GetMessageFileSource(fileName, messageId, (ChatMessageType)type);
        return File(bytes, Constants.FileResponseHeader, fileName);
    }
}