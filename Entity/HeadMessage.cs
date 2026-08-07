using FreeSql.DataAnnotations;
using SqlSugar;

namespace QQLike.Entity;

public class HeadMessage
{
    /// <summary>
    /// id,uuid
    /// </summary>
    [Column(IsPrimary = true)]
    [SugarColumn(IsPrimaryKey = true)]
    public string Id { get; set; }
    /// <summary>
    /// 用户Id
    /// </summary>
    public string UserId { get; set; }
    /// <summary>
    /// 联系人id
    /// </summary>
    public string ContactId { get; set; }
    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime {get; set;}
    /// <summary>
    /// 最后一次时间
    /// </summary>
    public DateTime? LastMessageTime {get; set;}
}