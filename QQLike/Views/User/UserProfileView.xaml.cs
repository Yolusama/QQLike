using System.Windows;
using System.Windows.Controls;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Views.User;

public partial class UserProfileView : UserControl
{
    private UserProfileViewModel ViewModel => (UserProfileViewModel)DataContext;

    public UserProfileView()
    {
        InitializeComponent();
        this.SetViewModel<UserProfileViewModel, UserProfileView>();
        Visibility = Visibility.Collapsed;
    }

    private void UserProfileView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
            ViewModel.LoadDataCommand.Execute(null);
        else
            ViewModel.UnloadCommand.Execute(null);
    }
}