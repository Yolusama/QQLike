using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using QQLike.Entity.Enum;

namespace QQLike.Domain;

public partial class ChatMessageItem : ObservableObject
{
    [ObservableProperty] 
    private string _avatar = string.Empty;
    [ObservableProperty] 
    private string _displayName = string.Empty;
    [ObservableProperty] 
    private string _content = string.Empty;
    [ObservableProperty] 
    private string _timeText = string.Empty;
    [ObservableProperty]
    private DateTime? _messageTime = null;
    [ObservableProperty]
    private bool _isSelf;
    [ObservableProperty]
    private ChatMessageType _messageType = ChatMessageType.Text;
    [ObservableProperty]
    private string _fileName = string.Empty;
    [ObservableProperty]
    private string _messageTimeText = string.Empty;
    [ObservableProperty]
    private string _groupMemberId = string.Empty;
    [ObservableProperty]
    private bool _isOwner = false;
    [ObservableProperty]
    private string _localSourcePath = string.Empty;
    [ObservableProperty]
    private string _source;

    [ObservableProperty] 
    private HorizontalAlignment _messageHorizontalAlignment = HorizontalAlignment.Left;
    [ObservableProperty]
    private HorizontalAlignment _bubbleHorizontalAlignment = HorizontalAlignment.Left;
    [ObservableProperty] 
    private Visibility _leftAvatarVisibility = Visibility.Visible;
    [ObservableProperty]
    private Visibility _rightAvatarVisibility = Visibility.Collapsed;
    [ObservableProperty]
    private Visibility _senderNameVisibility = Visibility.Visible;
    [ObservableProperty] 
    private Brush _bubbleBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
    [ObservableProperty] 
    private Brush _bubbleBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6E6E6"));
    [ObservableProperty]
    private TextAlignment _messageTimeAlignment = TextAlignment.Left;
    [ObservableProperty]
    private Visibility _contactNameVisibility = Visibility.Visible;
    
    public string UserId { get; set; }
    public string ContactId { get; set; }

    partial void OnIsSelfChanged(bool value)
    {
        if (value)
        {
            MessageHorizontalAlignment = HorizontalAlignment.Right;
            BubbleHorizontalAlignment = HorizontalAlignment.Right;
            LeftAvatarVisibility = Visibility.Collapsed;
            RightAvatarVisibility = Visibility.Visible;
            SenderNameVisibility = Visibility.Collapsed;
            BubbleBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9EEFF"));
            BubbleBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C2E3FF"));
            MessageTimeAlignment = TextAlignment.Right;
            return;
        }

        MessageHorizontalAlignment = HorizontalAlignment.Left;
        BubbleHorizontalAlignment = HorizontalAlignment.Left;
        LeftAvatarVisibility = Visibility.Visible;
        RightAvatarVisibility = Visibility.Collapsed;
        SenderNameVisibility = Visibility.Visible;
        BubbleBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        BubbleBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6E6E6"));
        MessageTimeAlignment = TextAlignment.Left;
    }
}
