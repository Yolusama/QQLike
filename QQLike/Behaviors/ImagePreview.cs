using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Components;
using QQLike.ViewModels;

namespace QQLike.Behaviors;

/// <summary>
/// 附加属性：为 Image 或使用 ImageBrush 的 Ellipse 等控件提供点击预览功能。
/// 当 Previewable 为 true 时，点击控件将打开 ImagePreviewer 窗口进行图片预览。
/// </summary>
public static class ImagePreview
{
    public static readonly DependencyProperty PreviewableProperty =
        DependencyProperty.RegisterAttached(
            "Previewable",
            typeof(bool),
            typeof(ImagePreview),
            new PropertyMetadata(false, OnPreviewableChanged));

    public static void SetPreviewable(DependencyObject obj, bool value)
        => obj.SetValue(PreviewableProperty, value);

    public static bool GetPreviewable(DependencyObject obj)
        => (bool)obj.GetValue(PreviewableProperty);

    private static void OnPreviewableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            if ((bool)e.NewValue)
            {
                element.Cursor = Cursors.Hand;
                element.MouseLeftButtonDown += OnElementClick;
            }
            else
            {
                element.Cursor = null;
                element.MouseLeftButtonDown -= OnElementClick;
            }
        }
    }

    private static void OnElementClick(object sender, MouseButtonEventArgs e)
    {
        var imageSource = ExtractImageSource(sender);
        if (imageSource == null) return;

        var previewer = App.ServiceProvider.GetRequiredService<ImagePreviewer>();
        var viewModel = (ImagePreviewerViewModel)previewer.DataContext;
        viewModel.ImageSource = imageSource;
        viewModel.Scale = 1.0;
        viewModel.RotationAngle = 0;
        var result = previewer.ShowDialog();
        if (result !=null && result.Value)
            previewer.Focus();
    }

    /// <summary>
    /// 从控件中提取图片源：
    /// 1. Image 控件 -> Image.Source
    /// 2. Shape (Ellipse等) -> Fill 为 ImageBrush 时的 ImageSource
    /// 3. 任意控件 -> Background 为 ImageBrush 时的 ImageSource
    /// </summary>
    private static ImageSource? ExtractImageSource(object sender)
    {
        switch (sender)
        {
            case Image image:
                return image.Source;

            case Shape shape when shape.Fill is ImageBrush fillBrush:
                return fillBrush.ImageSource;

            case Control control when control.Background is ImageBrush bgBrush:
                return bgBrush.ImageSource;

            default:
                return null;
        }
    }
}