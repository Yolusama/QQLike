using CommunityToolkit.Mvvm.ComponentModel;
using QQLike.Entity.VO;

namespace QQLike.Domain;

public  partial class UserContactManageItem : ObservableObject
{
    [ObservableProperty]
    private long _userContactGroupId;
    [ObservableProperty]
    private string _groupName;
    [ObservableProperty]
    private int _selectedGroupIndex;
    [ObservableProperty]
    private ValueLabel<long> _selectedGroup;
    
    public string UserId { get; set; }
    public string Nickname { get; set; }
    public string Avatar { get; set; }
    public string Remark { get; set; }
}
