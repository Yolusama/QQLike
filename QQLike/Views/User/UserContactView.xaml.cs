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
    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
            ViewModel.LoadUserContactGroupsCommand.Execute(null);
        else ViewModel.UnloadCommand.Execute(null);
    }
}