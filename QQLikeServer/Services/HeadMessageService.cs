using QQLike.Entity;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class HeadMessageService(IFreeSql orm,IRandomGenerator generator) : IHeadMessageService
{
    public async Task<ResponseResult<string>> Create(HeadMessageModel model)
    {
        using var worker = orm.CreateUnitOfWork();
        try
        {
            var headMessage = await orm.Select<HeadMessage>()
                .Where(e => e.UserId == model.UserId && e.ContactId == model.ContactId)
                .FirstAsync();
            if (headMessage == null)
            {
                headMessage = new HeadMessage
                {
                    Id = generator.Guid,
                    UserId = model.UserId,
                    ContactId = model.ContactId,
                    Content = string.Empty,
                    CreateTime = DateTime.Now,
                    LastMessageTime = DateTime.Now
                };
                await worker.Orm.Insert(headMessage).ExecuteAffrowsAsync();
            }
            else
            {
                headMessage.Content = model.Content;
                headMessage.LastMessageTime = model.LastMessageTime;
                await worker.Orm.Update<HeadMessage>()
                    .SetSource(headMessage)
                    .ExecuteAffrowsAsync();
            }
            worker.Commit();
            return ResponseResult<string>.OK(headMessage.Id);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            worker.Rollback();
            return ResponseResult.Fail($"创建消息头过程中程序出现异常:{e.Message}").Generic<string>();
        }
       
    }

    public async Task<ResponseResult<List<V_HeadMessage>>> Get(string userId)
    {
        var res = await orm.Select<V_HeadMessage>()
            .Where(v=>v.UserId == userId)
            .OrderByDescending(v=>v.LastMessageTime)
            .ToListAsync();
        
        

        return ResponseResult<List<V_HeadMessage>>.OK(res);
    }
}