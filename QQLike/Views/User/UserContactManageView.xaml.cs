using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QQLike.Domain;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Views.User;

public partial class UserContactManageView : Window
{
    private static UserContactManageView _holderWindow = null;
    private bool _loaded = false;
    private UserContactManageViewModel ViewModel => (UserContactManageViewModel)DataContext;
    public UserContactManageView(UserContactManageViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadDataCommand.Execute(null);
        _loaded = true;
    }

    private void CommonToolHeaderPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if(e.ButtonState == MouseButtonState.Pressed)
           DragMove();
    }

    public new void Show()
    {
        if (_holderWindow == null)
        {
            base.Show();
            _holderWindow = this;
        }
        else
            _holderWindow.Focus();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _holderWindow = null;
    }

    private void GroupSelector_OnDropDownClosed(object sender, EventArgs e)
    {
        if(!_loaded)return;
        if (sender is FrameworkElement { DataContext: UserContactManageItem item })
           ViewModel.ChangeGroupCommand.Execute(item);
    }

    private void ComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        GroupSelector_OnDropDownClosed(sender, e);
    }
}