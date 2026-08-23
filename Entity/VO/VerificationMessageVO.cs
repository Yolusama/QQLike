namespace QQLike.Entity.VO;

public class VerificationMessageVO
{
    public string Avatar { get; set; }
    public string Nickname { get; set; }
    public string UserId { get; set; }
    public DateTime? ApplyTime { get; set; }
    public int Status { get; set; }
    public string Source { get; set; }
    public string VerificationMessage { get; set; }
    public string ContactId { get; set; }
    public bool IsRead { get; set; }
}