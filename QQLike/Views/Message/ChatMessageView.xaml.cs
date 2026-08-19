using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
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

    private void ToggleMediaPlay(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle)
            return;

        var presenter = FindAncestor<ContentPresenter>(toggle);
        var media = presenter is null ? null : FindDescendant<MediaElement>(presenter);
        if (media is null || media.Source is null)
            return;

        if (toggle.IsChecked == true)
            media.Play();
        else
            media.Pause();

        UpdatePlayIcon(toggle);
    }

    private static void UpdatePlayIcon(ToggleButton toggle)
    {
        if (toggle.Content is PackIcon icon)
            icon.Kind = toggle.IsChecked == true ? PackIconKind.Pause : PackIconKind.Play;
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;

            var result = FindDescendant<T>(child);
            if (result is not null)
                return result;
        }

        return null;
    }
}