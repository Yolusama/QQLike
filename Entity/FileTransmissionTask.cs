using FreeSql.DataAnnotations;
using SqlSugar;

namespace QQLike.Entity;

/// <summary>
/// 大文件断点续传任务表
/// </summary>
public class FileTransmissionTask
{
    /// <summary>
    /// id，自增
    /// </summary>
    [Column(IsPrimary = true,IsIdentity = true),SugarColumn(IsPrimaryKey = true,IsIdentity = true)]
    public long Id { get; set; }
    /// <summary>
    /// 断点续传临时文件名
    /// </summary>
    public string TempFileName  { get; set; }
    /// <summary>
    /// 当前分片
    /// </summary>
    public int Current {get; set;}
    /// <summary>
    /// 总分片
    /// </summary>
    public int Total {get; set;}
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime {get; set;}
    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? FinishTime {get; set;}
}