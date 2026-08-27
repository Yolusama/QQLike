using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

/// <summary>
/// 群组创建 + 好友分组显示管理试图
/// </summary>
public partial class UserContactGroupView : Window
{
    private UserContactGroupViewModel ViewModel => this.GetViewModel<UserContactGroupViewModel>();
    public UserContactGroupView(UserContactGroupViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }

    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // TextBox.Text default source update is LostFocus; force update for immediate search.
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }

        ViewModel.SearchUsersCommand.Execute(null);
    }

    private void UserContactGroupView_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadCommand.Execute(null);
    }

    private void UserContactGroupView_OnClosed(object? sender, EventArgs e)
    {
        ViewModel.CloseCommand.Execute(null);
    }
}