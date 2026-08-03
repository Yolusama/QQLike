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

    private void VerificationMessageView_OnLoaded(object sender, RoutedEventArgs e)
    {
        var viewModel = this.GetViewModel<VerificationMessageViewModel>();
        viewModel.LoadNoticesCommand.Execute(null);
    }
}