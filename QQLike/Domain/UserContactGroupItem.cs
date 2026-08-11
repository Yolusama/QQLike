using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QQLike.Entity.VO;

namespace QQLike.Domain;

/// <summary>
/// TreeView 绑定的联系人分组项，包含分组及其下的联系人列表
/// </summary>
public partial class UserContactGroupItem : ObservableObject
{
    [ObservableProperty] private long _contactGroupId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private ObservableCollection<UserContactInfoItem> _userContacts = [];
    [ObservableProperty] private double _expandIconAngle;
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private long _userContactCount;
}
