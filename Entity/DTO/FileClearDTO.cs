namespace QQLike.Entity.DTO;

public struct FileClearDTO
{
    public long TransmissionId { get; set; }
    public FileInfo File { get; set; }
    public DirectoryInfo RootDirectory { get; set; }
}