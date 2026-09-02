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
    /// 视频存储路径
    /// </summary>
    public string AudioPath { get; set; } 
    /// <summary>
    /// 一般文件储存
    /// </summary>
    public string CommonPath { get; set; }
    /// <summary>
    /// 临时文件目录
    /// </summary>
    public string TempPath { get; set; }
    /// <summary>
    /// 文件缓存时间间隔（天）到时间自动删除
    /// </summary>
    public int FileExpireDays { get; set; }
    /// <summary>
    /// 临时文件缓存时间间隔（天）到时间自动删除
    /// </summary>
    public int TempFileExpireDays { get; set; }

    /// <summary>
    /// 最大文件大小（字节），默认 100MB
    /// </summary>
    public long MaxFileSize { get; set; }
}