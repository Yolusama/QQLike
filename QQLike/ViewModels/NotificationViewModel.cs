using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using QQLike.Components;
using QQLike.Entity.Enum;

namespace QQLike.ViewModels;

public partial class NotificationViewModel : ViewModelBase<NotificationComponent>
{
    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private double _offset = 16;

    [ObservableProperty]
    private MessageType _messageType = MessageType.Info;

    [ObservableProperty]
    private long _duration = 2500;

    [ObservableProperty]
    private HorizontalAlignment _side = HorizontalAlignment.Right;

    public Brush BackgroundBrush => MessageType switch
    {
        MessageType.Success => new SolidColorBrush(Color.FromRgb(0xF0, 0xF9, 0xEB)),
        MessageType.Warning => new SolidColorBrush(Color.FromRgb(0xFD, 0xF6, 0xEC)),
        MessageType.Error => new SolidColorBrush(Color.FromRgb(0xFE, 0xF0, 0xF0)),
        _ => new SolidColorBrush(Color.FromRgb(0xED, 0xF2, 0xFC))
    };

    public Brush ForegroundBrush => MessageType switch
    {
        MessageType.Success => new SolidColorBrush(Color.FromRgb(0x67, 0xC2, 0x3A)),
        MessageType.Warning => new SolidColorBrush(Color.FromRgb(0xE6, 0xA2, 0x3C)),
        MessageType.Error => new SolidColorBrush(Color.FromRgb(0xF5, 0x6C, 0x6C)),
        _ => new SolidColorBrush(Color.FromRgb(0x90, 0x93, 0x99))
    };

    public Brush BorderBrush => MessageType switch
    {
        MessageType.Success => new SolidColorBrush(Color.FromRgb(0xE1, 0xF3, 0xD8)),
        MessageType.Warning => new SolidColorBrush(Color.FromRgb(0xFA, 0xEC, 0xD8)),
        MessageType.Error => new SolidColorBrush(Color.FromRgb(0xFD, 0xE2, 0xE2)),
        _ => new SolidColorBrush(Color.FromRgb(0xEB, 0xEE, 0xF5))
    };

    partial void OnMessageTypeChanged(MessageType value)
    {
        _ = value;
        OnPropertyChanged(nameof(BackgroundBrush));
        OnPropertyChanged(nameof(ForegroundBrush));
        OnPropertyChanged(nameof(BorderBrush));
    }
}