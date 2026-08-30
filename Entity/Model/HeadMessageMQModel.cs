namespace QQLike.Entity.Model;

public class HeadMessageMQModel
{
    public string HeadMessageId { get; set; }
    public string UserId  { get; set; }
    public string ContactId { get; set; }
    public string Content { get; set; }
    public DateTime? LastMessageTime { get; set; }
}