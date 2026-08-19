namespace QQLike.Entity.VO;

public class ChatMessageVO
{
    /// <summary>
    /// 头像（Base64字符串或URL）
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
    /// 文件名（当消息包含附件时）
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 消息类型（如：文本、图片、文件等）
    /// </summary>
    public int MessageType { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 联系人姓名
    /// </summary>
    public string ContactName { get; set; }
}