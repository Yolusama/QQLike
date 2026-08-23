using System.Windows;
using System.Windows.Controls;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

/// <summary>
/// 群组创建 + 好友分组显示管理试图
/// </summary>
public partial class UserContactGroupView : UserControl
{
    public UserContactGroupView()
    {
        InitializeComponent();
        this.SetViewModel<UserContactGroupViewModel,UserContactGroupView>();
    }

    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        return;
    }
}