namespace QQLike.Entity.DTO;

public class FileTypeMessageDTO
{
    public string FileName { get; set; }
    public string FileExtension { get; set; }
    public byte[] FileBytes { get; set; }
    public string TempMessage { get; set; }
    public string OriginalFileName { get; set; }
}