using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.DTO;
using QQLike.Entity.Enum;
using QQLike.Entity.Result;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class ChatGroupService(
    IFreeSql orm,
    IRabbitMQProducer mqProducer,
    IProjectLogger logger,
    IRandomGenerator generator) : IChatGroupService
{
    public async Task<ResponseResult<string>> CreateChatGroup(CreateChatGroupDTO dto)
    {
        using var worker = orm.CreateUnitOfWork();
        try
        {
            var chatGroup = new ChatGroup
            {
                OwnerId = dto.CreatorId,
                Name = dto.GroupName,
                GroupNum = generator.GenerateByNumbers(10),
                Id = generator.Guid,
                CreateTime = DateTime.Now,
                Avatar = "default-group.png",
                DeleteMark = 0,
                CurrentCount = 1,
                MaxCount = 20
            };
            await worker.Orm.Insert(chatGroup).ExecuteAffrowsAsync();
            if (dto.ChosenUserIds.Count > 0)
            {
                var userContacts = new List<UserContact>();
                dto.ChosenUserIds.Add(dto.CreatorId);
                foreach (var userId in dto.ChosenUserIds)
                {
                    var userContact = new UserContact();
                    userContact.UserId = userId;
                    userContact.DeleteMark = 0;
                    userContact.UserContactGroupId = dto.UserContactGroupId;
                    userContact.ContactStatus = UserContactStatus.正常.GetValue();
                    userContact.IsGroup = true;
                    userContact.ContactId = chatGroup.Id;
                    userContacts.Add(userContact);
                }
                if(userContacts.Count > 0)
                    await worker.Orm.Insert(userContacts).ExecuteAffrowsAsync();
                foreach (var userContact in userContacts)
                {
                    var messageBody = new MQMessageBody
                    {
                        Identifier = userContact.UserId,
                        Body = chatGroup,
                        Muted = true
                    };
                    await mqProducer.Produce(Constants.CreateChatGroupQueue, Constants.MQExchange,
                        $"{Constants.CreateChatGroupQueue}_{userContact.UserId}", messageBody.ToNormalJson());
                }
            }
            
            worker.Commit();
            return ResponseResult<string>.OK(chatGroup.Id);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"创建群聊过程中程序出现异常:{e.Message}", "群聊服务");
            worker.Rollback();
            return ResponseResult.Fail("创建群聊失败").Generic<string>();
        }
    }
}