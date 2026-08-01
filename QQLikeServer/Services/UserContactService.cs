using QQLike.Entity;
using QQLike.Entity.Model;
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class UserContactService(IFreeSql orm) : IUserContactService
{
    public async Task<ResponseResult<List<long>>> GetUserContactGroups(string userId)
    {
        var groupIds = await orm.Select<UserContactGroup>()
            .Where(ucg => ucg.UserId == userId)
            .ToListAsync(e=>e.Id);
        return ResponseResult<List<long>>.OK("获取成功", groupIds);
    }

    public async Task<ResponseResult<List<UserContactGroupingVO>>> GetUserContactGrouping(string userId)
    {
        var data = await orm.Select<UserContactGroup, UserContact>()
            .InnerJoin(e => e.t1.Id == e.t2.UserContactGroupId && 
                            !e.t1.IsGroup && !e.t2.IsGroup)
            .Where(e=>e.t2.UserId == userId)
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
}