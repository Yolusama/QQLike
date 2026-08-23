namespace QQLike.Entity;

public class V_ChatGroupMessage
{
    /// <summary>
    /// 消息ID
    /// </summary>
    public string MessageId { get; set; }

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
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 文件名（针对文件消息）
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 消息类型（1-文本 2-图片 3-文件 等）
    /// </summary>
    public int MessageType { get; set; }

    /// <summary>
    /// 发送者用户ID
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 用户昵称
    /// </summary>
    public string NickName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 是否自己发送的消息
    /// </summary>
    public bool IsSelf { get; set; }

    /// <summary>
    /// 群组ID（如果是群聊则有值）
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// 头消息ID
    /// </summary>
    public string HeadMessageId { get; set; }

    /// <summary>
    /// 群组显示名称
    /// </summary>
    public string GroupDisplayName { get; set; }
    
    /// <summary>
    /// 群成员ID
    /// </summary>
    public string GroupMemberId { get; set; }

    /// <summary>
    /// 是否为群主（1-是，0-否）
    /// </summary>
    public bool IsOwner { get; set; }
    /// <summary>
    /// 是否为在线消息
    /// </summary>
    public bool IsOnline { get; set; }
}