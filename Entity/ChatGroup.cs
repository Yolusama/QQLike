namespace QQLike.Entity;

public class ChatGroup
{
    /// <summary>
    /// id
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// 群号
    /// </summary>
    public string GroupNum { get; set; }
    /// <summary>
    /// 群主ID
    /// </summary>
    public string OwnerId { get; set; }
    /// <summary>
    /// 群名称
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// 群头像
    /// </summary>
    public string Avatar { get; set; }
    /// <summary>
    /// 群描述
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// 当前人数
    /// </summary>
    public int CurrentCount { get; set; }
    /// <summary>
    /// 最大人数
    /// </summary>
    public int MaxCount { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }
    /// <summary>
    /// 分类ID
    /// </summary>
    public long? CategoryId { get; set; }
    /// <summary>
    /// 删除标记
    /// </summary>
    public int DeleteMark { get; set; } = 0;
}