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
        var data = await orm.Select<VerificationMessage,User,User>()
            .InnerJoin(e=>e.t1.UserId==e.t2.Id)
            .InnerJoin(e=>e.t1.ContactId==e.t3.Id)
            .WhereIf(isGroup!=null,e=>e.t1.IsGroup==isGroup)
            .Where(e=>e.t1.UserId==userId)
            .OrderByDescending(e=>e.t1.CreateTime)
            .ToListAsync(e=>new  VerificationMessageVO
            {
                UserId = e.t2.Id,
                ContactId = e.t3.Id,
                VerificationMessage = e.t1.Message,
                ApplyTime = e.t1.CreateTime,
                Nickname = e.t3.Nickname,
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
                Remark =  model.Remark,
                UserContactGroupId = model.UserContactGroupId,
                Expire = (long)TimeSpan.FromDays(7).TotalMilliseconds // 设置过期时间为7天后
            };
            var entity1 = entity.MapTo(new VerificationMessage());
            entity1.ContactId = entity.UserId;
            entity1.UserId = model.ContactId;
            entity1.Remark = string.Empty;
            entity1.Status = VerificationMessageStatus.待验证.GetValue();
            entity1.UserContactGroupId = null;
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
            return ResponseResult.Fail($"添加验证消息过程中程序出现异常:{e.Message}");
        }
    }
}