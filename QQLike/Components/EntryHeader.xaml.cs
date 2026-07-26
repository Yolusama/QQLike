using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class EntryHeader : UserControl
{
    public bool MinimizeButtonVisible
    {
        get => (bool)GetValue(MinimizeButtonVisibleProperty);
        set => SetValue(MinimizeButtonVisibleProperty, value);
    }
    public static readonly DependencyProperty MinimizeButtonVisibleProperty =
        DependencyProperty.Register(nameof(MinimizeButtonVisible), typeof(bool), typeof(EntryHeader), new PropertyMetadata(true));

    public EntryHeader()
    {
        InitializeComponent();
        this.SetViewModel<EntryHeaderViewModel, EntryHeader>(); 
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            Window.GetWindow(this)?.DragMove();
    }
}