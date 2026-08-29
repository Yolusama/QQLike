namespace QQLike.Entity.DTO;

public class CreateChatGroupDTO
{
    public string GroupName { get; set; }
    public string CreatorId { get; set; }
    public string GroupCreatorName { get; set; }
    public long UserContactGroupId { get; set; }
    public List<string> ChosenUserIds { get; set; }
}