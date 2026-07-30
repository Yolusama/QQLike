using FreeSql.DataAnnotations;
using SqlSugar;

namespace QQLike.Entity;

/// <summary>
/// 用户表
/// </summary>
public class User
{
    /// <summary>
    /// ID GUID值
    /// </summary>
    [Column(IsPrimary = true)]
    [SugarColumn(IsPrimaryKey = true)]
    public string Id { get; set; }
    /// <summary>
    /// 账号
    /// </summary>
    public string Account { get; set; }
    /// <summary>
    /// 昵称
    /// </summary>
    public string Nickname { get; set; }
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// 邮箱
    /// </summary>
    public string Email { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }
    /// <summary>
    /// 性别
    /// </summary>
    public int Gender {get;set;}
    /// <summary>
    /// 个性签名
    /// </summary>
    public string Signature { get; set; }
    /// <summary>
    /// 生日
    /// </summary>
    public DateOnly? Birthday { get; set; }
    /// <summary>
    /// 省份
    /// </summary>
    public string Province { get; set; }
    /// <summary>
    /// 地区
    /// </summary>
    public string Region { get; set; }
    /// <summary>
    /// 上次登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }
    /// <summary>
    /// 头像
    /// </summary>
    public string Avatar { get; set; }
    [Column(DbType = "tinyint(1)")]
    [SugarColumn(ColumnDataType = "tinyint", IsNullable = true)]
    public bool? IsOnline { get; set; }
    //public string Nation {get;set;}
}