using QQLike.Entity.Configuration;
using QQLike.Functional.Instructure;
using QQLike.Services.Interfaces;
using QQLike.Views.User;

namespace QQLike.ViewModels;

public class UserProfileViewModel(SysSetting setting, ISessionStorage sessionStorage, IWindowFactory windowFactory, IApiService apiService) 
    : ViewModelBase<UserProfileView>
{
    
}