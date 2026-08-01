using System.Windows.Controls;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Views.Message;

public partial class VerificationMessageView : UserControl
{
    public VerificationMessageView()
    {
        InitializeComponent();
        this.SetViewModel<VerificationMessageViewModel,VerificationMessageView>();
    }
}