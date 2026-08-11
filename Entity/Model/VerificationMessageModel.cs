namespace QQLike.Entity.Model;

public class VerificationMessageModel
{
    public string UserId { get; set; }
    public string ContactId { get; set; }
    public string VerificationMessage { get; set; }
    public int Status { get; set; }
    public string Source { get; set; }
    public bool IsGroup { get; set; }
    public bool NeedConfirm { get; set; }
    public DateTime? ConfirmTime { get; set; }
    public long UserContactGroupId  { get; set; }
    public string Remark { get; set; }
}