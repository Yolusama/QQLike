using System.Text.Json;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class VerificationMessageService(IFreeSql orm,IRabbitMQProducer mqProducer) : IVerificationMessageService
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

    public async Task<ResponseResult> AddVerificationMessage(VerificationMessageModel model)
    {
        using var worker = orm.CreateUnitOfWork();
        try
        {
            var entity = new VerificationMessage
            {
                UserId = model.UserId,
                ContactId = model.ContactId,
                Message = model.VerificationMessage,
                Status = VerificationMessageStatus.验证中.GetValue(),
                Source = model.Source,
                CreateTime = DateTime.Now,
                IsGroup = model.IsGroup,
                NeedConfirm = model.NeedConfirm,
                Expire = (long)TimeSpan.FromDays(7).TotalMilliseconds // 设置过期时间为7天后
            };
            var entity1 = entity.MapTo(new VerificationMessage());
            entity1.ContactId = entity.UserId;
            entity1.UserId = model.ContactId;
            entity1.Status = VerificationMessageStatus.待验证.GetValue();
            await orm.Insert(new List<VerificationMessage> { entity, entity1 })
                .ExecuteAffrowsAsync();
            await mqProducer.Produce(nameof(VerificationMessage),Constants.MQExchange,
                nameof(VerificationMessage),JsonSerializer.Serialize(entity1));
            worker.Commit();
            return ResponseResult.OK("添加成功");
        }
        catch (Exception e)
        {
            worker.Rollback();
            Console.WriteLine(e);
            throw;
        }
    }
}