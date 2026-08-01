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
    private MDMenuItem? _selectedMenuItem = new MDMenuItem{Activated = false};
    [ObservableProperty]
    private ObservableCollection<MDMenuItem> _menuItems = [
        new MDMenuItem { Title = "消息", SelectedIcon = PackIconKind.MessageText, UnselectedIcon = PackIconKind.MessageTextOutline },
        new MDMenuItem { Title = "联系人", SelectedIcon = PackIconKind.AccountGroup, UnselectedIcon = PackIconKind.AccountGroupOutline },
        new MDMenuItem { Title = "验证消息", SelectedIcon = PackIconKind.MessageAlert, UnselectedIcon = PackIconKind.MessageAlertOutline},
        new MDMenuItem { Title = "设置", SelectedIcon = PackIconKind.Cog, UnselectedIcon = PackIconKind.CogOutline }
    ];

    partial void OnSelectedMenuItemChanged(MDMenuItem? value)
    {
        if (value == null) return;
        SelectedMenuItem = value;
        foreach (var item in MenuItems)
            item.Activated = false;
        SwitchPage(value.Title);
    }

    private void SwitchPage(string title)
    {
        switch (title)
        {
            case "消息":
                break;
            case "联系人":
                if (SelectedMenuItem != null)
                    SelectedMenuItem.Activated = true;
                break;
            case "验证消息":
                break;
            case "设置":
                break;
        }
    }

    [RelayCommand]
    private void OpenUserProfile()
    {
        
    }
}
