using System.Windows;
using System.Windows.Controls;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Views.Message;

public partial class ChatMessageView : UserControl
{
    private ChatMessageViewModel ViewModel => this.GetViewModel<ChatMessageViewModel>();
    public ChatMessageView()
    {
        InitializeComponent();
        this.SetViewModel<ChatMessageViewModel,ChatMessageView>();
    }

    private void ChatMessageView_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
            ViewModel.LoadDataCommand.Execute(null);
    }
}