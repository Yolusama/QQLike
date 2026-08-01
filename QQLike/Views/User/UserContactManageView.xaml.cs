using System.Windows;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Views.User;

public partial class UserContactManageView : Window
{
    private UserContactManageViewModel ViewModel => (UserContactManageViewModel)DataContext;
    public UserContactManageView(UserContactManageViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        
    }
}