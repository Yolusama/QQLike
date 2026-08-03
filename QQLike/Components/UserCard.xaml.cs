using System.Windows;
using System.Windows.Controls;
using QQLike.Entity.DTO;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class UserCard : UserControl
{
    public static readonly DependencyProperty UserIdProperty = DependencyProperty.Register(
        nameof(UserId), typeof(string), typeof(UserCard), new PropertyMetadata(default(string), OnCardParameterChanged));
    public static readonly DependencyProperty InGroupProperty = DependencyProperty.Register(
        nameof(InGroup), typeof(bool), typeof(UserCard), new PropertyMetadata(default(bool), OnCardParameterChanged));
    public static readonly DependencyProperty GroupIdProperty = DependencyProperty.Register(
        nameof(GroupId), typeof(string), typeof(UserCard), new PropertyMetadata(default(string), OnCardParameterChanged));

    public string UserId
    {
        get =>(string)GetValue(UserIdProperty); 
        set => SetValue(UserIdProperty, value); 
    }

    public bool InGroup
    {
        get => (bool)GetValue(InGroupProperty);
        set => SetValue(InGroupProperty, value);
    }

    public string GroupId
    {
        get => (string)GetValue(GroupIdProperty);
        set => SetValue(GroupIdProperty, value);
    }
    
    public UserCard()
    {
        InitializeComponent();
        this.SetViewModel<UserCardViewModel,UserCard>();
    }

    private static void OnCardParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UserCard card)
            card.RefreshCard();
    }

    private void  RefreshCard()
    {
        if (DataContext is UserCardViewModel vm)
            vm.LoadCardCommand.Execute(new UserCardDTO
            {
                UserId = UserId,
                GroupId = GroupId,
                InGroup = InGroup
            });
    }

    private void UserCard_OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshCard();
    }
}