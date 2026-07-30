using System.Windows;
using System.Windows.Controls;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class UserSearchHeader : UserControl
{
    private readonly UserSearchHeaderViewModel _viewModel;
    public UserSearchHeader()
    {
        InitializeComponent();
        this.SetViewModel<UserSearchHeaderViewModel,UserSearchHeader>();
        _viewModel = (UserSearchHeaderViewModel)DataContext;
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelSearch();
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchCommand.Execute(null);
    }
}