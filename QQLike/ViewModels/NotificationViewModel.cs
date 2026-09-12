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
    private NotificationType _messageType =NotificationType.Info;

    [ObservableProperty]
    private long _duration = 2500;

    [ObservableProperty]
    private HorizontalAlignment _side = HorizontalAlignment.Right;

    public Brush BackgroundBrush => MessageType switch
    {
        NotificationType.Success => new SolidColorBrush(Color.FromRgb(0xF0, 0xF9, 0xEB)),
        NotificationType.Warning => new SolidColorBrush(Color.FromRgb(0xFD, 0xF6, 0xEC)),
        NotificationType.Error => new SolidColorBrush(Color.FromRgb(0xFE, 0xF0, 0xF0)),
        _ => new SolidColorBrush(Color.FromRgb(0xED, 0xF2, 0xFC))
    };

    public Brush ForegroundBrush => MessageType switch
    {
        NotificationType.Success => new SolidColorBrush(Color.FromRgb(0x67, 0xC2, 0x3A)),
        NotificationType.Warning => new SolidColorBrush(Color.FromRgb(0xE6, 0xA2, 0x3C)),
        NotificationType.Error => new SolidColorBrush(Color.FromRgb(0xF5, 0x6C, 0x6C)),
        _ => new SolidColorBrush(Color.FromRgb(0x90, 0x93, 0x99))
    };

    public Brush BorderBrush => MessageType switch
    {
        NotificationType.Success => new SolidColorBrush(Color.FromRgb(0xE1, 0xF3, 0xD8)),
        NotificationType.Warning => new SolidColorBrush(Color.FromRgb(0xFA, 0xEC, 0xD8)),
        NotificationType.Error => new SolidColorBrush(Color.FromRgb(0xFD, 0xE2, 0xE2)),
        _ => new SolidColorBrush(Color.FromRgb(0xEB, 0xEE, 0xF5))
    };

    partial void OnMessageTypeChanged(NotificationType value)
    {
        _ = value;
        OnPropertyChanged(nameof(BackgroundBrush));
        OnPropertyChanged(nameof(ForegroundBrush));
        OnPropertyChanged(nameof(BorderBrush));
    }
}