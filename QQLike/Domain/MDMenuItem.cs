using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace QQLike.Domain;

public partial class MDMenuItem : ObservableObject
{
    public required string Title { get; set; }
    public PackIconKind SelectedIcon { get; set; }
    public PackIconKind UnselectedIcon { get; set; }
    public bool Activated { get; set; }
    
    [ObservableProperty]
    private string? _notification;
}