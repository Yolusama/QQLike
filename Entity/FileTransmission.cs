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
        return size >= 20L * Constants.MB;
    }
    
    public static int GetBufferSize(long size)
    {
        if (size >= 20L * Constants.MB && size < 100L * Constants.MB)
            return 20 * Constants.KB;
        else if (size >= 100L * Constants.MB && size < 500L * Constants.MB)
            return 40 * Constants.KB;
        else if (size >= 500L * Constants.MB && size < Constants.GB)
            return Constants.MB;
        else if (size >= Constants.GB && size < 4L * Constants.GB)
            return  10*Constants.MB;
        else
            return 20 * Constants.MB;
    }
    
    
    public static int GetTotal(long size)
    {
        var buffSize = GetBufferSize(size);
        return (int)Math.Ceiling((size * 1.0) / buffSize);
    }

    public static string GetMemoryText(long size)
    {
        if(size < Constants.KB)
            return $"{size}B";
        else if(size >= Constants.KB && size < Constants.MB)
            return $"{(size * 1.0 / Constants.KB):F1}KB";
        else if(size >= Constants.MB && size < Constants.GB)
            return $"{(size * 1.0 / Constants.MB):F1}MB";
        else
            return $"{(size * 1.0 / Constants.GB):F1}GB";
    }
}