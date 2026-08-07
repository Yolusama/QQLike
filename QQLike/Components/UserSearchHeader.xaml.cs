using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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
        // Let the next focused element settle; keep panel open when focus moves inside this control.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!IsKeyboardFocusWithin)
            {
                _viewModel.CancelSearch();
            }
        }), DispatcherPriority.Background);
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchCommand.Execute(null);
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowSearch();
        
    }

    private void Button_LostFocus(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!IsKeyboardFocusWithin)
            {
                _viewModel.CancelAddingCommand.Execute(null);
            }
        }), DispatcherPriority.Background);
    }
}