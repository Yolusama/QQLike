using QQLike.Entity.DTO;
using QQLike.Entity.Result;

namespace QQLike.Services.Interfaces;

public interface IChatGroupService
{
    public Task<ResponseResult<string>> CreateChatGroup(CreateChatGroupDTO dto);
}