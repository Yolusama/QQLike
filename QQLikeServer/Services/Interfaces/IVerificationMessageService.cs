using QQLike.Entity.Result;
using QQLike.Entity.VO;

namespace QQLike.Services.Interfaces;

public interface IVerificationMessageService
{
    public Task<ResponseResult<List<VerificationMessageVO>>> GetVerificationMessages(string userId,bool? isGroup);
}