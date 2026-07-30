using FreeSql.DataAnnotations;
using SqlSugar;

namespace QQLike.Entity;

/// <summary>
/// 联系人分组
/// </summary>
public class UserContactGroup
{
    /// <summary>
    /// id自增
    /// </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }
    /// <summary>
    /// 用户Id
    /// </summary>
    public string UserId { get; set; }
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }
    /// <summary>
    /// 是否是群组
    /// </summary>
    [Column(DbType = "tinyint(1)")]
    [SugarColumn(ColumnDataType = "tinyint(1)")]
    public bool IsGroup { get; set; }
}