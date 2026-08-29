using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.DTO;
using QQLike.Entity.Enum;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
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
            var groupName = string.IsNullOrEmpty(dto.GroupName) ? $"{dto.GroupCreatorName}的群聊" : $"{dto.GroupCreatorName},{dto.GroupName}";
            var chatGroup = new ChatGroup
            {
                OwnerId = dto.CreatorId,
                Name = groupName,
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
                var onlineStatusOptions = await orm.Select<User>()
                    .Where(e => dto.ChosenUserIds.Contains(e.Id))
                    .ToListAsync(e=>new {e.Id,e.IsOnline});
                List<HeadMessage> headMessages = [];
                foreach (var userContact in userContacts)
                {
                    var userStatus = onlineStatusOptions.FirstOrDefault(e => e.Id == userContact.UserId);
                    if(userStatus == null) continue;
                    var currentHeadMessageId = generator.Guid;
                    if (userStatus.IsOnline!=null && userStatus.IsOnline.Value)
                    {
                        var messageBody = new MQMessageBody
                        {
                            Identifier = userContact.UserId,
                            Body = new GroupCreatedHeadMessage
                            {
                                UserId = userContact.UserId,
                                GroupId = chatGroup.Id,
                                GroupName = chatGroup.Name,
                                GroupAvatar = chatGroup.Avatar,
                                CreateTime = chatGroup.CreateTime,
                                HeadMessageId = currentHeadMessageId,
                                IsOwner = userContact.UserId == chatGroup.OwnerId
                            },
                            Muted = true
                        };
                        await mqProducer.Produce(Constants.CreateChatGroupQueue, Constants.MQExchange,
                            $"{Constants.CreateChatGroupQueue}_{userContact.UserId}", messageBody.ToNormalJson());
                    }
                    var headMessage = new HeadMessage();
                    headMessage.Id = currentHeadMessageId;
                    headMessage.UserId = userContact.UserId;
                    headMessage.ContactId = chatGroup.Id;
                    headMessage.Content = string.Empty;
                    headMessage.CreateTime = DateTime.Now;
                    headMessage.LastMessageTime = DateTime.Now;
                    headMessages.Add(headMessage);
                }
                
                if(headMessages.Count > 0)
                    await worker.Orm.Insert(headMessages).ExecuteAffrowsAsync();
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