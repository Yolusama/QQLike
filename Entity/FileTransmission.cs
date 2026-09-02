using FreeSql.DataAnnotations;
using SqlSugar;

namespace QQLike.Entity;

public class FileTransmission
{
    /// <summary>
    /// id自增
    /// </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; }
    /// <summary>
    /// 断点续传临时文件名
    /// </summary>
    public string TempFileName  { get; set; }
    /// <summary>
    /// 消息ID
    /// </summary>
    public long MessageId { get; set; }
    /// <summary>
    /// 头消息Id
    /// </summary>
    public string HeadMessageId { get; set; }
    /// <summary>
    /// 关联上传任务Id
    /// </summary>
    public long? TaskId { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }
}