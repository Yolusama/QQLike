using QQLike.Entity.Configuration;
using QQLike.Functional.Instructure;
using QQLike.Services.Interfaces;
using QQLike.Views.Group;

namespace QQLike.ViewModels;

public class GroupProfileViewModel(SysSetting setting, ISessionStorage sessionStorage, IWindowFactory windowFactory, IApiService apiService) : ViewModelBase<GroupProfileView>
{
    
}