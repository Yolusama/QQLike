using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QQLike.Functional.Instructure;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class AppHeader : UserControl
{
    public AppHeader()
    {
        InitializeComponent();
        this.SetViewModel<AppHeaderViewModel, AppHeader>();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            Window.GetWindow(this)?.DragMove();
    }
}