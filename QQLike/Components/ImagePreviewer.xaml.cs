using System.Windows;
using System.Windows.Input;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class ImagePreviewer : Window
{
    public ImagePreviewer(ImagePreviewerViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }

    // 标题栏拖拽
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
    
}