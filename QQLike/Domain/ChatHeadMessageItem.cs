using CommunityToolkit.Mvvm.ComponentModel;

namespace QQLike.Domain;

public partial class ChatHeadMessageItem : ObservableObject
{
    [ObservableProperty] private string _contactId = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _lastContent = string.Empty;
    [ObservableProperty] private string _timeText = string.Empty;
    [ObservableProperty] private string _avatar = string.Empty;
    /// <summary>
    /// 是否有头像（用于控制首字母占位）
    /// </summary>
    [ObservableProperty] private bool _hasAvatar;
    /// <summary>
    /// 无头像时的首字母占位
    /// </summary>
    [ObservableProperty] private string _avatarInitial = string.Empty;
    [ObservableProperty] private int _unreadCount;
    public string HeadMessageId { get; set; }
    public bool IsGroup {get; set;}
}

