namespace QQLike.Entity.Model;

public class UserRegisterModel
{
    public string? Account {get; set; }
    public string Password {get; set; }
    public string Nickname {get; set; }
    public string Email {get; set; }
    public int Gender {get; set; }
    public string? Avatar {get; set; }
    public string VerificationCode {get; set; }
}