using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using QQLike.Components;
using QQLike.Services.Interfaces;

namespace QQLike.ViewModels;

public partial class ScreenShotComponentViewModel(IScreenShotsHandler shotsHandler) : ViewModelBase<ScreenShotComponent>
{
    [ObservableProperty]
    private BitmapSource? _source;

    private Rect _selectionRect;

    [RelayCommand]
    private void Close()
    {
        View.Close();
    }

    [RelayCommand]
    private async Task CopyToClipboard()
    {
        if (Source == null || _selectionRect.Width < 1 || _selectionRect.Height < 1)
            return;

        var croppedBitmap = new CroppedBitmap(Source,
            new Int32Rect(
                0,
                0,
                (int)_selectionRect.Width,
                (int)_selectionRect.Height
            ));
        var shotsImage = croppedBitmap.Clone();

        Clipboard.SetImage(croppedBitmap);
        await shotsHandler.StoreAsync(shotsImage);
        Close();
    }

    [RelayCommand]
    private void Save()
    {
        if (Source == null || _selectionRect.Width < 1 || _selectionRect.Height < 1)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "PNG 图片|*.png|JPEG 图片|*.jpg|BMP 图片|*.bmp",
            DefaultExt = ".png",
            FileName = $"ScreenShot{DateTime.Now:yyyyMMddHHmmss}"
        };

        if (dialog.ShowDialog() == true)
        {
            var croppedBitmap = new CroppedBitmap(Source,
                new Int32Rect(
                    0,
                    0,
                    (int)_selectionRect.Width,
                    (int)_selectionRect.Height
                ));

            BitmapEncoder encoder;
            var ext = System.IO.Path.GetExtension(dialog.FileName).ToLower();
            if (ext == ".jpg" || ext == ".jpeg")
                encoder = new JpegBitmapEncoder();
            else if (ext == ".bmp")
                encoder = new BmpBitmapEncoder();
            else
                encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));

            using var fileStream = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Create);
            encoder.Save(fileStream);
        }

        Close();
    }

    [RelayCommand]
    private void CopyColor()
    {
        // 使用 View 的 GetPixelColor 方法获取颜色（从保存的屏幕像素数据中读取）
        var source = PresentationSource.FromVisual(View);
        var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        
        // 将 DIP 坐标转换为物理像素坐标
        var pixelX = (int)(View.CurrentPoint.X * dpiX);
        var pixelY = (int)(View.CurrentPoint.Y * dpiY);

        var (r, g, b) = View.GetPixelColor(pixelX, pixelY);
        
        if (r == 0 && g == 0 && b == 0)
            return;

        var hexColor = $"#{r:X2}{g:X2}{b:X2}";
        Clipboard.SetText(hexColor);
        View.Close();
    }

    public void SetSelectionRect(Rect selectionRect)
    {
        _selectionRect = selectionRect;
    }
}