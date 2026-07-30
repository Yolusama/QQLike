using FreeSql.DataAnnotations;
using SqlSugar;

namespace QQLike.Entity;

/// <summary>
/// 验证消息
/// </summary>
public class VerificationMessage
{
    /// <summary>
    /// id自增
    /// </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }
    /// <summary>
    /// 是否为群组验证信息
    /// </summary>
    [Column(DbType = "tinyint(1)"),SugarColumn(ColumnDataType = "tinyint")]
    public bool IsGroup { get; set; }
    /// <summary>
    /// 联系方ID 人/群组
    /// </summary>
    public string ContactId { get; set; }
    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; }
    /// <summary>
    /// 是否为需要确认的消息
    /// </summary>
    [Column(DbType = "tinyint(1)"),SugarColumn(ColumnDataType = "tinyint")]
    public bool NeedConfirm { get; set; }
    /// <summary>
    /// 来源
    /// </summary>
    public string Source { get; set; }
    /// <summary>
    /// 过期时间，不设置会一直挂着
    /// </summary>
    public long? Expire { get; set; }
    /// <summary>
    /// 消息状态,1.验证中,2.同意,3.拒绝,4.忽略,5.过期
    /// </summary>
    public int Status { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }
}