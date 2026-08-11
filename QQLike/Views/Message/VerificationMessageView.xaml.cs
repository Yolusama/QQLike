using System.Windows;
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

    private void VerificationMessageView_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var viewModel = this.GetViewModel<VerificationMessageViewModel>();
        if ((bool)e.NewValue)
            viewModel.LoadNoticesCommand.Execute(null);
    }
}