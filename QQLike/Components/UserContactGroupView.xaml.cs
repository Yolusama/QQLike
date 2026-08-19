using System.Windows;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

/// <summary>
/// 群组创建 + 好友分组显示管理试图
/// </summary>
public partial class UserContactGroupView : Window
{
    public UserContactGroupView(UserContactGroupViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }
}