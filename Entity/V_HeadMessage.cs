namespace QQLike.Entity;

public class V_HeadMessage
{
    public string HeadMessageId { get; set; }
    public string UserId { get; set; }
    public string ContactId { get; set; }
    public string ContactName { get; set; }
    public string Avatar { get; set; }
    public string Remark { get; set; }
    public bool IsGroup { get; set; }
    public string Content { get; set; }
    public int ContactStatus { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
}