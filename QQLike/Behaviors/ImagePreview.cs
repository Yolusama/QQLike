using System.Windows;

namespace QQLike.Behaviors;

public static class ImagePreview
{
    private static readonly DependencyProperty PreviewableProperty = DependencyProperty.Register(
        nameof(Previewable), typeof(bool), typeof(ImagePreview), new PropertyMetadata(default(bool)));

    public static bool Previewable { get; set; }
    
    public static void SetIsPreviewable(DependencyObject obj, bool value)
        => obj.SetValue(PreviewableProperty, value);

    public static bool GetIsPreviewable(DependencyObject obj)
        => (bool)obj.GetValue(PreviewableProperty);

    private static void OnImageClick()
    {
        Previewable = true;
    }
}