using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Functional.Instructure;

namespace QQLike.Behaviors;

public static class ImageDefault
{
    private const string DefaultImageIcon = "default-image.png";
    private const string WithDefaultPropertyName = "WithDefault";
    
    public static readonly DependencyProperty WithDefaultProperty = DependencyProperty.RegisterAttached(
        WithDefaultPropertyName, typeof(bool), typeof(ImageDefault), new PropertyMetadata(false, OnWithDefaultChanged));

    public static void SetWithDefault(DependencyObject obj, bool value)
        => obj.SetValue(WithDefaultProperty, value);

    public static bool GetWithDefault(DependencyObject obj)
        => (bool)obj.GetValue(WithDefaultProperty);
    

    private static void OnWithDefaultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var enabled = e.NewValue is true;

        if (d is Image image)
        {
            if (enabled)
            {
                image.ImageFailed += OnImageFailed;
                image.Loaded += OnElementLoaded;
                TryApplyDefaultImage(image, force: false);
            }
            else
            {
                image.ImageFailed -= OnImageFailed;
                image.Loaded -= OnElementLoaded;
            }
            return;
        }

        if (d is FrameworkElement element)
        {
            if (enabled)
            {
                element.Loaded += OnElementLoaded;
                TryApplyDefaultImage(d, force: false);
            }
            else
                element.Loaded -= OnElementLoaded;
        }
    }

    private static void OnImageFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        if (sender is DependencyObject target)
            TryApplyDefaultImage(target, force: true);
    }

    private static void OnElementLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is DependencyObject target)
            TryApplyDefaultImage(target, force: false);
    }

    private static void TryApplyDefaultImage(DependencyObject target, bool force)
    {
        var source = CreateDefaultImageSource();
        if (source is null) return;

        switch (target)
        {
            case Image image when force || image.Source is null:
                image.Source = source;
                break;
            case Shape shape when shape.Fill is ImageBrush fillBrush && (force || fillBrush.ImageSource is null):
                fillBrush.ImageSource = source;
                break;
            case Control control when control.Background is ImageBrush bgBrush && (force || bgBrush.ImageSource is null):
                bgBrush.ImageSource = source;
                break;
        }
    }

    private static ImageSource? CreateDefaultImageSource()
    {
        var sourceHandler = App.ServiceProvider.GetRequiredService<IUserChatSourceHandler>();
        var imageUrl = sourceHandler.ImageUrl(DefaultImageIcon);
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;
        return new BitmapImage(new Uri(imageUrl, UriKind.RelativeOrAbsolute));
    }
}