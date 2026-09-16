using Microsoft.AspNetCore.Mvc;
using QQLike.Entity.Attributes;
using QQLike.Entity.Common;
using QQLike.Entity.DTO;
using QQLike.Entity.Enum;
using QQLike.Entity.Result;
using QQLike.Services.Interfaces;

namespace QQLike.Controllers;

[Route("api/[controller]/[action]")]
public class ChatMessageController(IChatMessageService chatMessageService) : ControllerBase
{
    [RequestAuthorize]
    [HttpGet]
    public async Task<ActionResult> GetMessageFileSource([FromQuery]string fileName,
        [FromQuery]long messageId, [FromQuery]int type)
    {
        var bytes = await chatMessageService.GetMessageFileSource(fileName,  (ChatMessageType)type);
        return File(bytes, Constants.FileResponseHeader, fileName);
    }
    
    [RequestAuthorize]
    [HttpPost]
    public async Task<ActionResult<ResponseResult<bool>>> UploadFile()
    {
        var file = Request.Form.Files[0];
        var tempFileName = Request.Form["tempFileName"].ToString();
        var taskId = long.Parse(Request.Form["taskId"].ToString());
        var current = int.Parse(Request.Form["current"].ToString());
        var total = int.Parse(Request.Form["total"].ToString());
        var bufferSize = long.Parse(Request.Form["bufferSize"].ToString());
        var messageType = Enum.Parse<ChatMessageType>(Request.Form["messageType"].ToString());
        
        var result = await chatMessageService.UploadFile(file, messageType,
            taskId, tempFileName,
            current, total,
            bufferSize);
        return Ok(result);
    }

    [RequestAuthorize]
    [HttpGet]
    public async Task<ActionResult<ResponseResult<byte[]>>> DownloadFile([FromQuery] MessageFileDownloadDTO model)
    {
        return Ok(await chatMessageService
            .DownloadFile(model.Type,model.TaskId,model.FileName,model.Current,model.Total,model.BufferSize));
    }

    [RequestAuthorize]
    [HttpDelete]
    public async Task<ActionResult<ResponseResult>> RemoveTempFile([FromQuery] string fileName,[FromQuery]int messageType)
    {
        return Ok(await chatMessageService.RemoveTempFile(fileName, (ChatMessageType)messageType));
    }
}