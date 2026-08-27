using CommunityToolkit.Mvvm.ComponentModel;

namespace QQLike.Domain;

public class UserContactGroupViewItem : UserContactInfoItem
{
    private bool _isSelected;
    private string _contactName;
    private string _accountText;
    private string _contactToolTipText;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
        }
    }
    
    public string ContactName
    {
        get => _contactName;
        set
        {
            _contactName = value;
            OnPropertyChanged();
        }
    }

    public string AccountText
    {
        get => _accountText;
        set
        {
            _accountText = value;
            OnPropertyChanged();
        }
    }

    public string ContactToolTipText
    {
        get => _contactToolTipText;
        set
        {
            _contactToolTipText = value;
            OnPropertyChanged();
        }
    }
}