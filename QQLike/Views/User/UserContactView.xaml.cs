using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QQLike.Domain;
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

    private void OnTreeViewItemExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem { DataContext: UserContactGroupItem item })
        {
            ViewModel.UserGroupExpandCommand.Execute(item);
        }
    }

    private void OnTreeViewItemCollapsed(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem { DataContext: UserContactGroupItem item })
        {
            ViewModel.UserGroupExpandCommand.Execute(item);
        }
    }
    
}