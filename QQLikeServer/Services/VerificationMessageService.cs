using QQLike.Entity;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class VerificationMessageService(IFreeSql orm) : IVerificationMessageService
{
    public async Task<ResponseResult<List<VerificationMessageVO>>> GetVerificationMessages(string userId, bool? isGroup)
    {
        var data = await orm.Select<VerificationMessage,User>()
            .InnerJoin(e=>e.t1.UserId==e.t2.Id)
            .WhereIf(isGroup!=null,e=>e.t1.IsGroup==isGroup)
            .Where(e=>e.t1.UserId==userId)
            .ToListAsync(e=>new  VerificationMessageVO
            {
                UserId = e.t1.UserId,
                VerificationMessage = e.t1.Message,
                ApplyTime = e.t1.CreateTime,
                Nickname = e.t2.Nickname,
                Avatar = e.t2.Avatar,
                Status = e.t1.Status,
                Source = e.t1.Source
            });
        return ResponseResult<List<VerificationMessageVO>>.OK(data);
    }
}