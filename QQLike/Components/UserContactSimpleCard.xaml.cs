using System.Windows.Controls;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class UserContactSimpleCard : UserControl
{
    public UserContactSimpleCard()
    {
        InitializeComponent();
        this.SetViewModel<UserContactSimpleCardViewModel, UserContactSimpleCard>();
    }
}