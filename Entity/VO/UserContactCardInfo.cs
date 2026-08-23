namespace QQLike.Entity.VO;

public class UserContactCardInfo
{
    public string? Account { get; set; }
    public string? Nickname { get; set; }
    public string? Signature { get; set; }
    public string Avatar { get; set; }
    public DateOnly? Birthday { get; set; }
    public bool? IsOnline { get; set; }
    public string? Remark { get; set; }
    public string? Region { get; set; }
    public bool IsFriend  { get; set; }
    public int Gender { get; set; }
    public string? GroupNum { get; set; }
    public string? GroupDescription { get; set; }
}