using QQLike.Entity.Enum;

namespace QQLike.Entity.DTO;

public class MessageFileDownloadDTO
{
    public string FileName { get; set; }
    public long TaskId { get; set; }
    public ChatMessageType Type { get; set; }
    public int Current { get; set; }
    public int Total { get; set; }
    public long BufferSize  { get; set; }
}