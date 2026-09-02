using Microsoft.AspNetCore.Mvc;
using QQLike.Services.Interfaces;

namespace QQLike.Controllers;

[Route("api/[controller]")]
public class ChatMessageController(IChatMessageService chatMessageService) : ControllerBase
{
    
}