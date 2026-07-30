using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Services.Interfaces;

namespace QQLike.Domain;

public partial class UserContactItem : ObservableObject
{
    [ObservableProperty]
    private string _contactName;
    [ObservableProperty]
    private string _avatar;
    [ObservableProperty]
    private string _groupFrom;
    
    public string ContactIdentifier {get; init; }
    
    private readonly IUserControlFactory _userControlFactory;

    public UserContactItem()
    {
        _userControlFactory = App.ServiceProvider.GetRequiredService<IUserControlFactory>();
    }
    
    [RelayCommand]
    private void GoUserProfile()
    {
        
    }
    [RelayCommand]
    private void GoGroupProfile()
    {
        
    }

    [RelayCommand]
    private void GoUserMessage()
    {
        
    }
    [RelayCommand]
    private void GoGroupMessage()
    {
        
    }
}