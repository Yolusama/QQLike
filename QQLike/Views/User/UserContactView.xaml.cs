using System.Windows;
using System.Windows.Controls;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Views.User;

public partial class UserContactView : UserControl
{
    private UserContactViewModel ViewModel  => DataContext as UserContactViewModel;
    public UserContactView()
    {
        InitializeComponent();
        this.SetViewModel<UserContactViewModel,UserContactView>();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
       ViewModel.LoadUserContactGroupsCommand.Execute(null);
    }
}