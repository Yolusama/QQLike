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
    /*public static readonly DependencyProperty IsGroupProperty = DependencyProperty.Register(nameof(IsGroup),
        typeof(bool), typeof(UserContactSimpleCard), new PropertyMetadata(false, null));
    public static readonly DependencyProperty UserIdProperty = DependencyProperty.Register(nameof(UserId),
        typeof(string), typeof(UserContactSimpleCard), new PropertyMetadata(string.Empty, null));
    
    public bool IsGroup { get => (bool)GetValue(IsGroupProperty); set => SetValue(IsGroupProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }*/
    
    public UserContactSimpleCard()
    {
        InitializeComponent();
        this.SetViewModel<UserContactSimpleCardViewModel, UserContactSimpleCard>();
    }

  

    private void UserContactSimpleCard_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if(!(bool)e.NewValue)return;
        this.GetViewModel<UserContactSimpleCardViewModel>().LoadUserDataCommand.Execute(null);
    }
    
    
    
}