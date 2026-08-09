using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Entity.Enum;
using QQLike.Functional.Utils;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class MessageComponent : Window
{
    private MessageViewModel ViewModel => (MessageViewModel)DataContext;

    public MessageComponent(MessageViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    public static void ShowMessage(Window owner, string message, MessageType type = MessageType.Info, long duration = 2000, double offset = 16)
    {
        var component = App.ServiceProvider.GetRequiredService<MessageComponent>();
        var viewModel = (MessageViewModel)component.DataContext;
        viewModel.Message = message;
        viewModel.MessageType = type;
        viewModel.Duration = duration;
        viewModel.Offset = offset;
        component.Owner = owner;
        component.Show();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Owner is not null)
        {
            Owner.LocationChanged += OnOwnerLayoutChanged;
            Owner.SizeChanged += OnOwnerLayoutChanged;
        }

        UpdateLayout();
        UpdatePosition();

        if (ViewModel.Duration <= 0)
        {
            return;
        }

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(ViewModel.Duration)
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };

        timer.Start();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        if (Owner is not null)
        {
            Owner.LocationChanged -= OnOwnerLayoutChanged;
            Owner.SizeChanged -= OnOwnerLayoutChanged;
        }
    }

    private void OnOwnerLayoutChanged(object? sender, EventArgs e)
    {
        UpdatePosition();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MessageViewModel.Offset))
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        if (Owner is not null)
        {
            Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
            Top = Owner.Top + ViewModel.Offset;
            return;
        }

        Left = (SystemParameters.WorkArea.Width - ActualWidth) / 2 + SystemParameters.WorkArea.Left;
        Top = SystemParameters.WorkArea.Top + ViewModel.Offset;
    }
}