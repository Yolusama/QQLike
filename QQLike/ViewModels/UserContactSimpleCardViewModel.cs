using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.Enum;
using QQLike.Entity.Model;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services;

namespace QQLike.ViewModels;

public partial class UserContactSimpleCardViewModel(IApiService apiService,ISessionStorage sessionStorage,SysSetting setting) 
    : ViewModelBase<UserContactSimpleCard>
{
    [ObservableProperty] 
    private string _avatar;
    [ObservableProperty]
    private string _nickname;
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
    [ObservableProperty]
    private string _region = string.Empty;
    [ObservableProperty]
    private string _birthdayText = string.Empty;
    [ObservableProperty]
    private bool _isFriend;
    [ObservableProperty]
    private bool _isMale;
    [ObservableProperty]
    private bool _isFemale;
    [ObservableProperty]
    private bool _isOnline;
    [ObservableProperty]
    private bool _isOffline;
    [ObservableProperty]
    private Visibility _visible = Visibility.Collapsed;

    public string UserId { get; set; }
    public bool IsGroup { get; set; }


    [RelayCommand]
    private async Task LoadUserData()
    {
        var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        try
        {
            if(IsGroup)
                IsGroupView = true;
            else
                IsUserView = true;
            if (IsUserView)
            {
                var res = await apiService.GetAsync<UserContactCardInfo>
                    ($"api/{nameof(User)}/GetUserContactCardInfo/{user.UserId}/{UserId}", null);
                if (res.Success)
                {
                    Account = $"QQLike {res.Data.Account}";
                    Nickname = res.Data.Nickname;
                    Signature = res.Data.Signature;
                    Remark = res.Data.Remark;
                    Region = res.Data.Region;
                    IsOnline = res.Data.IsOnline ?? false;
                    IsOffline = !IsOnline;
                    StatusText = IsOnline ? "在线" : "离线";
                    BirthdayText = res.Data.Birthday?.ToString("yyyy年MM月dd日");
                    IsMale = res.Data.Gender == 0;
                    IsFemale = !IsMale;
                    Avatar = $"{setting.ApiUrl}/Files/Images/{res.Data.Avatar}";
                }
                else
                    MessageComponent.ShowMessage(Owner, $"加载用户数据失败:{res.Message}", MessageType.Error);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner,$"加载用户数据失败:{e.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    private async Task OpenMessaging()
    {
        var mainViewModel = Owner.GetViewModel<MainViewModel>();
        sessionStorage.Set(CachingKeys.ChatMessageCurrentHeadId, UserId);
        try
        {
            var model = new HeadMessageModel()
            {
                UserId = sessionStorage.Get<UserLoginVO>(CachingKeys.User).UserId,
                ContactId = UserId
            };
            var res = await apiService
                .PutAsync<string>($"api/{nameof(HeadMessage)}/Create", model);
            if(res.Success)
            {
                sessionStorage.Set(CachingKeys.ChatMessageCurrentHeadId, res.Data);
                mainViewModel.ShowMenu(nameof(ChatMessage));
            }
            else 
                MessageComponent.ShowMessage(Owner, res.Message, MessageType.Error);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(Owner, $"程序异常：{e.Message}", MessageType.Error);
        }
   
    }

    [RelayCommand]
    private void Share()
    {
        
    }
    
    
}