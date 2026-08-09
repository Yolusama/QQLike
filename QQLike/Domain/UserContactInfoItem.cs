using System.ComponentModel;
using System.Runtime.CompilerServices;
using QQLike.Entity.VO;

namespace QQLike.Domain;

public class UserContactInfoItem :UserContactInfo, INotifyPropertyChanged
{
    private string _remark;
    
    public new string Remark
    {
        get => _remark;
        set
        {
            if (_remark != value)
            {
                _remark = value;
                OnPropertyChanged();
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}