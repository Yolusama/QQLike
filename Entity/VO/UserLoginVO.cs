namespace QQLike.Entity.VO;

public class UserLoginVO
{
    public string UserId { get; set; }
    public string Account {get; set; }
    public string Password {get; set; }
    public string Nickname {get; set; }
    public int Gender {get; set; }
    public string Avatar {get; set; }
    public DateTime? LastLoginTime {get; set; }
    public string Token {get; set; }
    public string Signature {get; set; }
}