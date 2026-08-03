using CommunityToolkit.Mvvm.ComponentModel;
using QQLike.Entity.Enum;

namespace QQLike.Domain;

public partial class VerificationMessageItem : ObservableObject
{
    public string Avatar { get; set; }
    public string Nickname { get; set; }
    public string UserId { get; set; }
    public string ContactId { get; set; }
    public DateTime? ApplyTime { get; set; }
    public string Source { get; set; }
    public string VerificationMessage { get; set; }
    public bool IsGroup { get; set; }
    public string DateText => ApplyTime?.ToString("yyyy-MM-dd") ?? string.Empty;

    [ObservableProperty]
    private int _status;

    public bool IsPending => Status == 0;

    public string StatusText => Status switch
    {
        0 => "待验证",
        1 => "验证中",
        2 => "同意",
        3 => "已通过",
        4 => "拒绝",
        5 => "被拒绝",
        6 => "忽略",
        7 => "过期",
        _ => string.Empty
    };

    partial void OnStatusChanged(int value)
    {
        OnPropertyChanged(nameof(IsPending));
        OnPropertyChanged(nameof(StatusText));
    }
}