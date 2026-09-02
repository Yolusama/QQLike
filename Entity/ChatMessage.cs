using FreeSql.DataAnnotations;
using SqlSugar;

namespace QQLike.Entity;

public class ChatMessage
{
    /// <summary>
    /// id自增
    /// </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    public long Id { get; set; }
    /// <summary>
    /// 用户id
    /// </summary>
    public string UserId {get; set;}
    /// <summary>
    /// 对方id
    /// </summary>
    public string ContactId {get; set;}
    /// <summary>
    /// 群聊中发言人id
    /// </summary>
    public string GroupMemberId {get; set;}
    /// <summary>
    /// 根据小写类型，文本类输出文本，文件类保存到缓存地址
    /// </summary>
    public string Content {get; set;}
    /// <summary>
    /// 消息类型
    /// </summary>
    public int MessageType {get; set;}
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime {get; set;}
    /// <summary>
    /// 文件名
    /// </summary>
    public string? FileName { get; set; }
    /// <summary>
    /// 本地保存地址
    /// </summary>
    public string?  LocalSourceName { get; set; }
    /// <summary>
    /// 上传时原文件名
    /// </summary>
    public string?  OriginalFileName { get; set; }
    /// <summary>
    /// 关联头信息Id
    /// </summary>
    public string HeadMessageId {get; set;}
    [Column(DbType = "tinyint(1)"),SugarColumn(ColumnDataType = "tinyint(1)")]
    public bool IsRead {get; set;}
    [Column(DbType = "tinyint(1)"),SugarColumn(ColumnDataType = "tinyint(1)")]
    public bool IsSelf {get; set;}
    [Column(DbType = "tinyint(1)"),SugarColumn(ColumnDataType = "tinyint(1)")]
    public bool IsOnline { get; set; }
    /// <summary>
    /// 文件消息时携带
    /// </summary>
    [Column(IsIgnore = true),SugarColumn(IsIgnore = true)]
    public byte[] FileBytes { get; set; }
}