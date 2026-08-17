using QQLike.Entity;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;

namespace QQLike.Services.Interfaces;

public interface IHeadMessageService
{
    public Task<ResponseResult<string>> Create(HeadMessageModel model);
    public Task<ResponseResult<List<V_HeadMessage>>> Get(string userId);
}