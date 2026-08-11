namespace QQLike.Entity.VO;

public class UserProfileVO
{
    public string UserId { get; set; }
    public int? Gender { get; set; }
    public string Nickname { get; set; }
    public DateOnly Birthday { get; set; }
    public string Avatar { get; set; }
    public string Remark { get; set; }
    public string Signature { get; set; }
    public string Account { get; set; }
    public long UserContactGroupId { get; set; }
}