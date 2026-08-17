using Microsoft.AspNetCore.Mvc;
using QQLike.Entity;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Services.Interfaces;

namespace QQLike.Controllers;

[Route("api/[controller]/[action]")]
public class HeadMessageController(IHeadMessageService headMessageService) : ControllerBase
{
    [HttpPut]
    public async Task<ResponseResult<string>> Create([FromBody]HeadMessageModel model)
    {
        return await headMessageService.Create(model);
    }
    
    [HttpGet("{userId}")]
    public async Task<ResponseResult<List<V_HeadMessage>>> Get([FromRoute]string userId)
    {
        return await headMessageService.Get(userId);
    }
}