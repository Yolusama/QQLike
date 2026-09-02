using QQLike.Entity.Enum;

namespace QQLike.Entity.Model;

public class FileTypeMessageModel
{
    public string FileName  { get; set; }
    public ChatMessageType Type { get; set; }
    public byte[] FileBytes { get; set; }
}