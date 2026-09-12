using FreeSql.DataAnnotations;
using QQLike.Entity.Common;
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
    [Column(DbType = "TINYINT(1)"),SugarColumn(ColumnDataType = "TINYINT(1)")]
    public bool IsValid { get; set; }
    /// <summary>
    /// 是否为接收否
    /// </summary>
    [Column(DbType = "TINYINT(1)"),SugarColumn(ColumnDataType = "TINYINT(1)")]
    public bool IsReceiveSide { get; set; }

    public static bool NeedTask(long size)
    {
        return size > 20L * Constants.MB;
    }
    
    public static int GetBufferSize(long size)
    {
        if (size >= 20L * Constants.MB && size < 100L * Constants.MB)
            return 8 * Constants.KB;
        else if (size >= 100L * Constants.MB && size < 500L * Constants.MB)
            return 20 * Constants.KB;
        else if (size >= 500L * Constants.MB && size < Constants.GB)
            return 40 * Constants.KB;
        else if (size >= Constants.GB && size < 4L * Constants.GB)
            return  Constants.MB;
        else
            return 10 * Constants.MB;
    }
    
    
    public static int GetTotal(long size)
    {
        var buffSize = GetBufferSize(size);
        return (int)Math.Ceiling((size * 1.0) / buffSize);
    }
}