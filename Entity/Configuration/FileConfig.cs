namespace QQLike.Entity.Configuration;

/// <summary>
/// 文件配置
/// </summary>
public class FileConfig
{
    /// <summary>
    /// 文件根目录
    /// </summary>
    public string FileRootPath { get; set; }

    /// <summary>
    /// 图片存储路径
    /// </summary>
    public string ImagePath { get; set; }

    /// <summary>
    /// 视频存储路径
    /// </summary>
    public string VideoPath { get; set; } 

    /// <summary>
    /// 最大文件大小（字节），默认 100MB
    /// </summary>
    public long MaxFileSize { get; set; }
}