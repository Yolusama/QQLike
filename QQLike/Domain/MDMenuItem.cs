using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace QQLike.Domain;

public partial class MDMenuItem : ObservableObject
{
    [ObservableProperty]
    private string _title;
    [ObservableProperty]
    private PackIconKind _selectedIcon;
    [ObservableProperty]
    private PackIconKind _unselectedIcon;
    [ObservableProperty]
    private bool _activated;
    [ObservableProperty]
    private string? _notification;
}