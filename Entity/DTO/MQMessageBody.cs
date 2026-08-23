namespace QQLike.Entity.DTO;

public class MQMessageBody
{
    public string Identifier { get; set; }
    public bool Muted { get; set; }
    public object Body { get; set; }
}