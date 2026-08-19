using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class UserContactSimpleCardViewModel(ISqlSugarClient sugarClient) : ViewModelBase<UserContactSimpleCard>
{
    [ObservableProperty] 
    private string _avatar;
    [ObservableProperty]
    private string _nickName;
    [ObservableProperty] 
    private string _signature;
    [ObservableProperty]
    private string _remark;
    [ObservableProperty]
    private string _account;
    [ObservableProperty] 
    private string _groupNum;
    [ObservableProperty] 
    private string _groupDescription;
    [ObservableProperty]
    private bool _isGroupView;
    [ObservableProperty] 
    private string _statusText;
    [ObservableProperty]
    private bool _isUserView;

    [RelayCommand]
    private void OpenMessaging()
    {
        
    }

    [RelayCommand]
    private void Share()
    {
        
    }
    
    
}