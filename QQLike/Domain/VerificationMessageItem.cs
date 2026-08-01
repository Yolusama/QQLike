namespace QQLike.Domain;

public class VerificationMessageItem
{
    public string Avatar { get; set; }
    public string Nickname { get; set; }
    public DateTime? ApplyTime { get; set; }
    public string Source { get; set; }
    public string VerificationMessage { get; set; }
    public int Status { get; set; }
}