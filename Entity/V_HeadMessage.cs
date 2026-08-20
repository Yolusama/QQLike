using SqlSugar;

namespace QQLike.Entity;

public class V_HeadMessage
{
    /// <summary>
    /// 头消息id
    /// </summary>
    public string HeadMessageId { get; set; }
    /// <summary>
    /// 用户id
    /// </summary>
    public string UserId { get; set; }
    /// <summary>
    /// 联系方id
    /// </summary>
    public string ContactId { get; set; }
    /// <summary>
    /// 联系方显示名称
    /// </summary>
    public string ContactName { get; set; }
    /// <summary>
    /// 头像
    /// </summary>
    public string Avatar { get; set; }
    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; }
    [SugarColumn(ColumnDataType = "tinyint(1)")]
    public bool IsGroup { get; set; }
    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; }
    /// <summary>
    /// 联系人状态
    /// </summary>
    public int ContactStatus { get; set; }
    /// <summary>
    /// 最后消息时间
    /// </summary>
    public DateTime? LastMessageTime { get; set; }
    /// <summary>
    /// 未读消息数
    /// </summary>
    public int UnreadCount { get; set; }
    /// <summary>
    /// 群显示名称
    /// </summary>
    public string GroupDisplayName { get; set; }
}