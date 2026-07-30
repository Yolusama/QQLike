using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using QQLike.Entity.VO;

namespace QQLike.Domain;

/// <summary>
/// TreeView 绑定的联系人分组项，包含分组及其下的联系人列表
/// </summary>
public partial class UserContactGroupItem : ObservableObject
{
    [ObservableProperty] private long _contactGroupId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private PackIcon? _icon;
    [ObservableProperty] private ObservableCollection<UserContactInfo> _userContacts = [];
    [ObservableProperty] private double _expandIconAngle;

    public bool IsExpanded { get; set; }
}
