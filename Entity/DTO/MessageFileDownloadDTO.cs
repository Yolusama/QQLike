using QQLike.Entity.Enum;

namespace QQLike.Entity.DTO;

public class MessageFileDownloadDTO
{
    public string FileName { get; set; }
    public ChatMessageType Type { get; set; }
    public long Current { get; set; }
    public long Total { get; set; }
    public long BufferSize  { get; set; }
}