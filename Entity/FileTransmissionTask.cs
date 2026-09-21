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
    #region  连续上传模型，数据上由于网络情况不可能达成完美的连续上传
    /*/// <summary>
    /// 当前分片
    /// </summary>
    public int Current {get; set;}
    /// <summary>
    /// 总分片
    /// </summary>
    public int Total {get; set;}*/
    #endregion
    
    /// <summary>
    /// 当前已上传
    /// </summary>
    public long Current {get; set;}
    /// <summary>
    /// 总大小
    /// </summary>
    public long Total {get; set;}
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime {get; set;}
    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? FinishTime {get; set;}
    /// <summary>
    /// 进行状态 1.完成 2.进行中 3.已取消,4.暂停
    /// </summary>
    [SugarColumn(ColumnDataType = "TINYINT(1)")]
    public int State {get; set;}
    /// <summary>
    /// 传输类型 1.上传 2.下载
    /// </summary>
    public int Type {get; set;}
    public string PercentStr()
    {
        var number = (Current * 1.0m / Total) * 100;
        return $"{Math.Round(number, 1):F1}%";
    }
}