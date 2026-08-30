using CommunityToolkit.Mvvm.ComponentModel;

namespace QQLike.Domain;

public partial class ChatHeadMessageItem : ObservableObject
{
    [ObservableProperty]
    private string _contactId = string.Empty;
    [ObservableProperty] 
    private string _displayName = string.Empty;
    [ObservableProperty] 
    private string _lastContent = string.Empty;
    [ObservableProperty] 
    private string _timeText = string.Empty;
    [ObservableProperty]
    private string _avatar = string.Empty;
    [ObservableProperty] 
    private int _unreadCount;
    public string HeadMessageId { get; set; }
    public bool IsGroup {get; set;}
    public bool IsUser => !IsGroup;
    public bool IsOwner {get; set;}
    public bool IsBlocked { get; set; }
    public bool MessageReceiveMuted {get; set;}
}


