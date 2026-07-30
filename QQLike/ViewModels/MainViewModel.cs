using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using QQLike.Domain;
using QQLike.Services.Interfaces;
using QQLike.Views;

namespace QQLike.ViewModels;

public partial class MainViewModel(IUserControlFactory userControlFactory) : ViewModelBase<MainView>
{
    [ObservableProperty]
    private MDMenuItem? _selectedMenuItem;

    public ObservableCollection<MDMenuItem> MenuItems { get; } = [
        new MDMenuItem { Title = "消息", SelectedIcon = PackIconKind.MessageText, UnselectedIcon = PackIconKind.MessageTextOutline },
        new MDMenuItem { Title = "联系人", SelectedIcon = PackIconKind.AccountGroup, UnselectedIcon = PackIconKind.AccountGroupOutline },
        new MDMenuItem { Title = "验证消息", SelectedIcon = PackIconKind.MessageAlert, UnselectedIcon = PackIconKind.MessageAlertOutline},
        new MDMenuItem { Title = "设置", SelectedIcon = PackIconKind.Cog, UnselectedIcon = PackIconKind.CogOutline }
    ];

    partial void OnSelectedMenuItemChanged(MDMenuItem? value)
    {
        if (value == null) return;
        foreach (var item in MenuItems)
            item.Activated = false;
        value.Activated = true;
        SwitchPage(value.Title);
    }

    private void SwitchPage(string title)
    {
        switch (title)
        {
            case "消息":
                // Switch to message page
                break;
            case "联系人":
                // Switch to contacts page
                break;
            case "验证消息":
                // Switch to verification messages page
                break;
            case "设置":
                // Switch to settings page
                break;
        }
    }

    [RelayCommand]
    private void OpenUserProfile()
    {
        
    }
}
