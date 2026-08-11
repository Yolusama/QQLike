using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class UserSearchHeader : UserControl
{
    private readonly UserSearchHeaderViewModel _viewModel;
    private bool _isMouseOverSearchPopup;
    private bool _isMouseOverAddPopup;

    public UserSearchHeader()
    {
        InitializeComponent();
        this.SetViewModel<UserSearchHeaderViewModel, UserSearchHeader>();
        _viewModel = (UserSearchHeaderViewModel)DataContext;
    }


    private void TryCloseSearchByMouseLeave(object sender, MouseEventArgs e)
    {
        SearchTextBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        _viewModel.CancelSearch();
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
            if (!IsKeyboardFocusWithin && !_isMouseOverAddPopup)
            {
                _viewModel.CancelAddingCommand.Execute(null);
            }
        }), DispatcherPriority.Background);
    }

    private void SearchPopup_Closed(object? sender, EventArgs e)
    {
        _isMouseOverSearchPopup = false;
        if (_viewModel.IsSearching)
        {
            _viewModel.CancelSearch();
        }
    }

    private void SearchPopup_MouseEnter(object sender, MouseEventArgs e)
    {
        _isMouseOverSearchPopup = true;
    }

    private void SearchPopup_MouseLeave(object sender, MouseEventArgs e)
    {
        _isMouseOverSearchPopup = false;
        TryCloseSearchByMouseLeave(sender, e);
    }

    private void SearchPopup_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isMouseOverSearchPopup = true;
    }

    private void SearchTextBox_MouseLeave(object sender, MouseEventArgs e)
    {
        TryCloseSearchByMouseLeave(sender, e);
    }

    private void AddPopup_MouseEnter(object sender, MouseEventArgs e)
    {
        _isMouseOverAddPopup = true;
    }

    private void AddPopup_MouseLeave(object sender, MouseEventArgs e)
    {
        _isMouseOverAddPopup = false;
        _viewModel.CancelAddingCommand.Execute(null);
    }
}