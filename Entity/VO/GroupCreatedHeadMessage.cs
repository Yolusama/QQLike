namespace QQLike.Entity.VO;

public class GroupCreatedHeadMessage
{
    public string HeadMessageId { get; set; }
    public string UserId { get; set; }
    public string GroupId { get; set; }
    public string GroupName { get; set; }
    public string GroupAvatar { get; set; }
    public DateTime? CreateTime { get; set; }
    public bool IsOwner { get; set; }
}