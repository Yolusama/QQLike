using FreeSql.DataAnnotations;
using SqlSugar;

namespace QQLike.Entity;

/// <summary>
/// 联系人关系表
/// </summary>
public class UserContact
{
    /// <summary>
    /// id自增
    /// </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }
    /// <summary>
    /// 用户ID，人/群组
    /// </summary>
    public string UserId { get; set; }
    /// <summary>
    /// 联系方ID，人/群组
    /// </summary>
    public string ContactId { get; set; }
    /// <summary>
    /// 是否为群组
    /// </summary>
    [Column(DbType = "tinyint(1)")]
    [SugarColumn(ColumnDataType = "tinyint(1)")]
    public bool IsGroup { get; set; }
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark {get; set;}
    /// <summary>
    /// 用户自分组Id
    /// </summary>
    public long UserContactGroupId { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }
    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime? DeleteTime { get; set; }
    /// <summary>
    /// 恢复时间
    /// </summary>
    public DateTime? RecoverTime { get; set; }
    /// <summary>
    /// 联系人状态,1.正常，2.删除，3.拉黑
    /// </summary>
    public int ContactStatus { get; set;}
    /// <summary>
    /// 删除标记用于恢复
    /// </summary>
    public int DeleteMark { get; set; } = 0;
    /// <summary>
    /// 群中昵称
    /// </summary>
    public string GroupDisplayName { get; set; }
    /// <summary>
    /// 消息免打扰
    /// </summary>
    [Column(DbType = "tinyint(1)")]
    [SugarColumn(ColumnDataType = "tinyint(1)")]
    public bool MessageReceiveMuted { get; set; }
}