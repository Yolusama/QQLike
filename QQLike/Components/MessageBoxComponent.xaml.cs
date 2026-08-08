using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Services;
using QQLike.ViewModels;
using MessageBoxOptions = QQLike.Entity.VO.MessageBoxOptions;

namespace QQLike.Components;

public partial class MessageBoxComponent : Window
{
    public MessageBoxComponent(MessageBoxViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }

    public void Show(Window owner, MessageBoxOptions options)
    {
        var viewModel = (MessageBoxViewModel)DataContext;
        Owner = owner;
        viewModel.Message = options.Message;
        viewModel.Title = options.Title;
        viewModel.ConfirmButtonText = options.ConfirmButtonText;
        viewModel.CancelButtonText = options.CancelButtonText;
        viewModel.ConfirmAction = options.ConfirmAction;
        viewModel.CancelAction = options.CancelAction;
        base.Show();
    }
    
    public static void ShowMessageBox(Window owner, MessageBoxOptions options)
    {
        var component = App.ServiceProvider.GetRequiredService<MessageBoxComponent>();
        component.Show(owner, options);
    }
}