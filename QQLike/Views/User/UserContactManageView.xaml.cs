using System.Windows;
using System.Windows.Input;
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
        ViewModel.LoadDataCommand.Execute(null);
    }

    private void CommonToolHeaderPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if(e.ButtonState == MouseButtonState.Pressed)
           DragMove();
    }
}