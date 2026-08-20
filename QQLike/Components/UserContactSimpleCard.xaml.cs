using System.Windows;
using System.Windows.Controls;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

/// <summary>
/// 消息界面，昵称点击的简单用户视图
/// </summary>
public partial class UserContactSimpleCard : UserControl
{
    public UserContactSimpleCard()
    {
        InitializeComponent();
        this.SetViewModel<UserContactSimpleCardViewModel, UserContactSimpleCard>();
    }

    private void UserContactSimpleCard_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var viewModel = this.GetViewModel<UserContactSimpleCardViewModel>();
    }
}