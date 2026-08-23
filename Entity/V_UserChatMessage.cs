using SqlSugar;

namespace QQLike.Entity;

public class V_UserChatMessage
{
    /// <summary>
    /// 消息Id
    /// </summary>
    public long MessageId { get; set; }
   /// <summary>
    /// 头像
    /// </summary>
    public string Avatar { get; set; }

    /// <summary>
    /// 联系人ID
    /// </summary>
    public string ContactId { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 文件名（针对文件消息）
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    public int MessageType { get; set; }

    /// <summary>
    /// 发送者用户ID
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 用户昵称（联系人名称）
    /// </summary>
    public string NickName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 头消息ID
    /// </summary>
    public string HeadMessageId { get; set; }
    
    /// <summary>
    /// 是否为自己发出的消息
    /// </summary>
    [SugarColumn(ColumnDataType = "tinyint(1)")]
    public bool IsSelf { get; set; }
    /// <summary>
    /// 是否为在线消息
    /// </summary>
    [SugarColumn(ColumnDataType = "tinyint(1)")]
    public bool IsOnline { get; set; }
}