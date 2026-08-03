using QQLike.Entity.Enum;

namespace QQLike.Entity.Model;

public class ChatMessageTransModel
{
    public string Message { get; set; }
    public ChatMessageType Type { get; set; }
    public object Data { get; set; }
}