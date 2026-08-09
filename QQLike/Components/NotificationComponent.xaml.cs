using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Entity.Enum;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class NotificationComponent : Window
{
    private const double HorizontalMargin = 16;
    private NotificationViewModel ViewModel => (NotificationViewModel)DataContext;

    public NotificationComponent(NotificationViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }
    
    public static void ShowNotification(
        Window owner,
        string message,
        MessageType type = MessageType.Info,
        long duration = 2500,
        double offset = 16,
        HorizontalAlignment side = HorizontalAlignment.Right)
    {
        var component = App.ServiceProvider.GetRequiredService<NotificationComponent>();
        var viewModel = (NotificationViewModel)component.DataContext;
        viewModel.Message = message;
        viewModel.MessageType = type;
        viewModel.Duration = duration;
        viewModel.Offset = offset;
        viewModel.Side = side;

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
        if (e.PropertyName == nameof(NotificationViewModel.Offset) ||
            e.PropertyName == nameof(NotificationViewModel.Side))
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        var onLeftSide = ViewModel.Side == HorizontalAlignment.Left;

        if (Owner is not null)
        {
            Left = onLeftSide
                ? Owner.Left + HorizontalMargin
                : Owner.Left + Owner.ActualWidth - ActualWidth - HorizontalMargin;
            Top = Owner.Top + ViewModel.Offset;
            return;
        }

        Left = onLeftSide
            ? SystemParameters.WorkArea.Left + HorizontalMargin
            : SystemParameters.WorkArea.Left + SystemParameters.WorkArea.Width - ActualWidth - HorizontalMargin;
        Top = SystemParameters.WorkArea.Top + ViewModel.Offset;
    }
}