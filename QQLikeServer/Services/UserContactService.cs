using QQLike.Entity;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class UserContactService(IFreeSql orm) : IUserContactService
{
    public async Task<ResponseResult<List<UserContactGroupVO>>> GetUserContactGroups(string userId,bool isGroup)
    {
        var groupIds = await orm.Select<UserContactGroup>()
            .Where(ucg => ucg.UserId == userId && ucg.IsGroup == isGroup)
            .ToListAsync(e=>new UserContactGroupVO
            {
                Id = e.Id,
                Name = e.Name
            });
        return ResponseResult<List<UserContactGroupVO>>.OK("获取成功", groupIds);
    }

    public async Task<ResponseResult<List<UserContactGroupingVO>>> GetUserContactGrouping(string userId)
    {
        var data = await orm.Select<UserContactGroup, UserContact>()
            .LeftJoin(e => e.t1.Id == e.t2.UserContactGroupId && 
                          !e.t2.IsGroup)
            .Where(e=>e.t1.UserId == userId && !e.t1.IsGroup)
            .GroupBy(e => new { e.t1.Id, e.t1.Name })
            .ToListAsync(e => new UserContactGroupingVO
            {
               GroupName = e.Value.Item1.Name,
               ContactGroupId = e.Value.Item1.Id,
               ContactCount = e.Count(e.Value.Item2.UserId)
            });
        return ResponseResult<List<UserContactGroupingVO>>.OK("获取成功", data);
    }

    public async Task<ResponseResult<long>> AddUserContactGroup(UserContactGroupModel model)
    {
        using var worker = orm.CreateUnitOfWork();
        try
        {
              var entity = new UserContactGroup
              {
                  Name = model.Name,
                  UserId = model.UserId,
                  CreateTime = DateTime.Now,
                  IsGroup = model.IsGroup
              };
              var id = await orm.Insert(entity).ExecuteIdentityAsync();
              worker.Commit();
              return ResponseResult<long>.OK("添加成功", id);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            worker.Rollback();
            return ResponseResult.Fail("添加失败").Generic<long>();
        }
    }

    public async Task<ResponseResult<List<UserContactManageVO>>> GetUserManageFriends(string userId,long userContactGroupId = 0)
    {
        var data = await orm.Select<User, UserContact, UserContactGroup>()
            .LeftJoin(e => e.t1.Id == e.t2.UserId && !e.t2.IsGroup)
            .LeftJoin(e => e.t2.UserContactGroupId == e.t3.Id)
            .WhereIf(userContactGroupId != 0, e => e.t3.Id == userContactGroupId)
            .Where(e => e.t2.UserId == userId)
            .ToListAsync(e => new UserContactManageVO
            {
                Avatar = e.t1.Avatar, Nickname = e.t1.Nickname, Remark = e.t2.Remark,
                UserId = e.t1.Id, UserContactGroupId = e.t3.Id
            });

        return ResponseResult<List<UserContactManageVO>>.OK("获取成功", data);
    }

    public async Task<ResponseResult<List<ValueLabel<long>>>> GetUserContactGroupSelections(string userId,bool isGroup)
    {
        var contactGroup = await orm.Select<UserContactGroup>()
            .Where(ucg => ucg.UserId == userId && ucg.IsGroup == isGroup)
            .ToListAsync(e => new ValueLabel<long>
            {
                Value = e.Id,
                Label = e.Name
            });
        return ResponseResult<List<ValueLabel<long>>>.OK("获取成功", contactGroup);
    }
}