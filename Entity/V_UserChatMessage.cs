using SqlSugar;

namespace QQLike.Entity;

public class V_UserChatMessage
{
    /// <summary>
    /// 消息Id
    /// </summary>
    public long MessageId { get; set; }
   /// <summary>
    /// 头像
    /// </summary>
    public string Avatar { get; set; }

    /// <summary>
    /// 联系人ID
    /// </summary>
    public string ContactId { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 文件名（针对文件消息）
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    public int MessageType { get; set; }

    /// <summary>
    /// 发送者用户ID
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 用户昵称（联系人名称）
    /// </summary>
    public string NickName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 头消息ID
    /// </summary>
    public string HeadMessageId { get; set; }
    
    /// <summary>
    /// 是否为自己发出的消息
    /// </summary>
    [SugarColumn(ColumnDataType = "tinyint(1)")]
    public bool IsSelf { get; set; }
    /// <summary>
    /// 是否为在线消息
    /// </summary>
    [SugarColumn(ColumnDataType = "tinyint(1)")]
    public bool IsOnline { get; set; }
    
    /// <summary>
    /// 本地保存地址
    /// </summary>
    public string?  LocalSourcePath { get; set; }
    /// <summary>
    /// 文件上传时的原名
    /// </summary>
    public string?  OriginalFileName { get; set; }
    
    /// <summary>
    /// 文件上传/下载进行状态 1.完成 2.进行中 3.已取消
    /// </summary>
    public int? ProcessState { get; set; }
    /// <summary>
    /// 当前切片
    /// </summary>
    public int? CurrentChunk { get; set; }
    /// <summary>
    /// 全部切片数
    /// </summary>
    public int? TotalChunkCount  { get; set; }
    /// <summary>
    /// 临时文件名
    /// </summary>
    public string TempFileName { get; set; }
    /// <summary>
    /// 文件大小
    /// </summary>
    public long? FileSize { get; set; } 
    /// <summary>
    /// 传输类型 1.上传 2.下载
    /// </summary>
    public int? FileTransType {get; set;}
}