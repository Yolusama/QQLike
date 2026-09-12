namespace QQLike.Entity.VO;

public struct FileClearVO
{
    public long TransmissionId { get; set; }
    public FileInfo File { get; set; }
    public DirectoryInfo RootDirectory { get; set; }
}