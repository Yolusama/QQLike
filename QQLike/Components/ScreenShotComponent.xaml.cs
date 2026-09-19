using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class ScreenShotComponent
{
    #region Win32 API

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest,
        int wDest, int hDest, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    private const int SRCCOPY = 0x00CC0020;

    #endregion

    private BitmapSource? _screenBitmap;
    private byte[]? _screenPixels;
    private int _screenPixelWidth;
    private int _screenPixelHeight;
    private int _screenStride;
    private Point _startPoint;
    private Point _currentPoint;
    public Point CurrentPoint => _currentPoint;
    private bool _isSelecting;
    private Rect _selectionRect;
    private bool _selectionComplete;

    public ScreenShotComponent(ScreenShotComponentViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
        Loaded += ScreenShotComponent_Loaded;
    }

    private void ScreenShotComponent_Loaded(object sender, RoutedEventArgs e)
    {
        // 使用 PrimaryScreen 获取包含任务栏的完整屏幕尺寸
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        var source = PresentationSource.FromVisual(this);
        var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        
        Width = screenWidth;
        Height = screenHeight;
        int physicalX = 0;
        int physicalY = 0;
        int physicalWidth = (int)(screenWidth * dpiX);
        int physicalHeight = (int)(screenHeight * dpiY);

        // 截取包含任务栏的完整屏幕
        var physicalBitmap = CaptureRegion(physicalX, physicalY, physicalWidth, physicalHeight);

        // 保存原始像素数据用于取色
        _screenPixelWidth = physicalBitmap.PixelWidth;
        _screenPixelHeight = physicalBitmap.PixelHeight;
        _screenStride = _screenPixelWidth * 4;
        _screenPixels = new byte[_screenStride * _screenPixelHeight];
        physicalBitmap.CopyPixels(_screenPixels, _screenStride, 0);

        // 缩回 DIP 尺寸
        var dipBitmap = new TransformedBitmap(
            physicalBitmap,
            new ScaleTransform(1.0 / dpiX, 1.0 / dpiY)
        );
        dipBitmap.Freeze();

        ScreenCaptureImage.Source = dipBitmap;
        
        Top = 0;
        Left = 0;
        
        ShowMagnifier();
        ModeTipBorder.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 截取主屏幕（纯 Win32 + WPF）
    /// </summary>
    private BitmapSource CaptureFullScreen()
    {
        var source = PresentationSource.FromVisual(this);
        var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        var width = (int)(SystemParameters.WorkArea.Width * dpiX);
        var height = (int)(SystemParameters.WorkArea.Height * dpiY);

        return CaptureRegion(0, 0, width, height);
    }

    /// <summary>
    /// 截取指定区域
    /// </summary>
    private static BitmapSource CaptureRegion(int x, int y, int width, int height)
    {
        IntPtr desktopWnd = GetDesktopWindow();
        IntPtr desktopDC = GetWindowDC(desktopWnd);
        IntPtr memoryDC = CreateCompatibleDC(desktopDC);
        IntPtr bitmap = CreateCompatibleBitmap(desktopDC, width, height);
        IntPtr oldBitmap = SelectObject(memoryDC, bitmap);

        try
        {
            // 拷贝屏幕像素到内存 DC
            BitBlt(memoryDC, 0, 0, width, height, desktopDC, x, y, SRCCOPY);

            // 将 GDI 位图转为 WPF BitmapSource
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            // 清理资源
            SelectObject(memoryDC, oldBitmap);
            DeleteObject(bitmap);
            DeleteDC(memoryDC);
            ReleaseDC(desktopWnd, desktopDC);
        }
    }

    private void ShowMagnifier()
    {
        MagnifierBorder.Visibility = Visibility.Visible;
        ColorInfoBorder.Visibility = Visibility.Visible;
        CoordInfoBorder.Visibility = Visibility.Visible;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        _currentPoint = pos;

        // 更新放大镜位置
        UpdateMagnifier(pos);

        // 更新颜色信息
        UpdateColorInfo(pos);

        // 更新坐标信息
        UpdateCoordInfo(pos);

        // 如果正在选择，更新选区
        if (_isSelecting)
        {
            UpdateSelection();
        }
    }

    private void UpdateMagnifier(Point pos)
    {
        var source = PresentationSource.FromVisual(this);
        var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        var pixelX = (int)(pos.X * dpiX);
        var pixelY = (int)(pos.Y * dpiY);
        
        // 放大镜显示 10x10 物理像素区域
        var captureSize = 10;
        
        // 计算缩放因子（确保是整数，以便像素边界清晰）
        var scaleFactor = Math.Max(1, (int)Math.Round(150 * dpiX / captureSize));
        var magnifierPhysicalSize = captureSize * scaleFactor;
        var magnifierDipSize = magnifierPhysicalSize / dpiX;
        var pixelDipSize = scaleFactor / dpiX;

        var left = pixelX - captureSize / 2;
        var top = pixelY - captureSize / 2;
        var captured = CaptureRegion(left, top, captureSize, captureSize);
        
        // 确保位图是 Pbgra32 格式
        if (captured.Format != PixelFormats.Pbgra32)
        {
            captured = new FormatConvertedBitmap(captured, PixelFormats.Pbgra32, null, 0);
        }
        
        // 提取像素数据
        var stride = captured.PixelWidth * 4;
        var pixels = new byte[stride * captured.PixelHeight];
        captured.CopyPixels(pixels, stride, 0);

        // 手动缩放每个像素到 scaleFactor x scaleFactor 的目标块
        var scaledPixels = new byte[magnifierPhysicalSize * magnifierPhysicalSize * 4];
        
        for (int y = 0; y < captureSize; y++)
        {
            for (int x = 0; x < captureSize; x++)
            {
                var srcIndex = (y * stride) + (x * 4);
                var b = pixels[srcIndex];
                var g = pixels[srcIndex + 1];
                var r = pixels[srcIndex + 2];
                var a = pixels[srcIndex + 3];
                
                // 填充 scaleFactor x scaleFactor 的目标块
                for (int dy = 0; dy < scaleFactor; dy++)
                {
                    for (int dx = 0; dx < scaleFactor; dx++)
                    {
                        var destX = x * scaleFactor + dx;
                        var destY = y * scaleFactor + dy;
                        var destIndex = (destY * magnifierPhysicalSize * 4) + (destX * 4);
                        scaledPixels[destIndex] = b;
                        scaledPixels[destIndex + 1] = g;
                        scaledPixels[destIndex + 2] = r;
                        scaledPixels[destIndex + 3] = a;
                    }
                }
            }
        }

        // 创建屏幕 DPI 的 WriteableBitmap
        var wbDpi = 96 * dpiX;
        var wb = new WriteableBitmap(magnifierPhysicalSize, magnifierPhysicalSize, wbDpi, wbDpi, PixelFormats.Pbgra32, null);
        wb.WritePixels(new Int32Rect(0, 0, magnifierPhysicalSize, magnifierPhysicalSize), scaledPixels, magnifierPhysicalSize * 4, 0);
        wb.Freeze();

        var image = new Image
        {
            Source = wb,
            Width = magnifierDipSize,
            Height = magnifierDipSize,
            Stretch = Stretch.None
        };

        MagnifierCanvas.Children.Clear();
        MagnifierCanvas.Children.Add(image);

        // 绘制像素网格线（使用 SnapToDevicePixels 确保清晰）
        for (int i = 0; i <= captureSize; i++)
        {
            var offset = i * pixelDipSize;
            var vLine = new Line
            {
                X1 = offset, Y1 = 0, X2 = offset, Y2 = magnifierDipSize,
                Stroke = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                StrokeThickness = 1,
                SnapsToDevicePixels = true
            };
            MagnifierCanvas.Children.Add(vLine);
            var hLine = new Line
            {
                X1 = 0, Y1 = offset, X2 = magnifierDipSize, Y2 = offset,
                Stroke = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                StrokeThickness = 1,
                SnapsToDevicePixels = true
            };
            MagnifierCanvas.Children.Add(hLine);
        }

        // 高亮中心像素（鼠标所在物理像素）
        var centerIndex = captureSize / 2;
        var centerOffset = centerIndex * pixelDipSize;
        var highlight = new Rectangle
        {
            Width = pixelDipSize,
            Height = pixelDipSize,
            Stroke = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            SnapsToDevicePixels = true
        };
        Canvas.SetLeft(highlight, centerOffset);
        Canvas.SetTop(highlight, centerOffset);
        MagnifierCanvas.Children.Add(highlight);

        // 更新放大镜尺寸
        MagnifierBorder.Width = magnifierDipSize;
        MagnifierBorder.Height = magnifierDipSize;
        MagnifierCanvas.Width = magnifierDipSize;
        MagnifierCanvas.Height = magnifierDipSize;

        // 更新放大镜位置
        var magnifierX = pos.X + 20;
        var magnifierY = pos.Y + 20;

        if (magnifierX + magnifierDipSize > ActualWidth)
            magnifierX = pos.X - magnifierDipSize - 20;
        if (magnifierY + magnifierDipSize > ActualHeight)
            magnifierY = pos.Y - magnifierDipSize - 20;

        Canvas.SetLeft(MagnifierBorder, magnifierX);
        Canvas.SetTop(MagnifierBorder, magnifierY);
    }

    private void UpdateColorInfo(Point pos)
    {
        var source = PresentationSource.FromVisual(this);
        var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        
        // 使用物理像素坐标获取颜色
        var pixelX = (int)(pos.X * dpiX);
        var pixelY = (int)(pos.Y * dpiY);

        // 获取像素颜色
        var croppedBitmap = CaptureRegion(pixelX, pixelY, 1, 1);
        
        // 确保位图是 Pbgra32 格式
        if (croppedBitmap.Format != PixelFormats.Pbgra32)
        {
            croppedBitmap = new FormatConvertedBitmap(croppedBitmap, PixelFormats.Pbgra32, null, 0);
        }
        
        var pixels = new byte[4];
        croppedBitmap.CopyPixels(pixels, 4, 0);

        var b = pixels[0];
        var g = pixels[1];
        var r = pixels[2];
        var a = pixels[3];

        var color = Color.FromArgb(a, r, g, b);
        var hexColor = $"#{r:X2}{g:X2}{b:X2}";

        // 更新颜色预览
        ColorPreviewBorder.Background = new SolidColorBrush(color);

        // 更新颜色文本
        ColorInfoText.Text = $"RGB({r},{g},{b}) {hexColor}";

        // 更新颜色信息面板位置
        var infoX = pos.X + 20;
        var infoY = pos.Y + 150;

        if (infoX + 200 > ActualWidth)
            infoX = pos.X - 220;
        if (infoY + 30 > ActualHeight)
            infoY = pos.Y - 50;

        Canvas.SetLeft(ColorInfoBorder, infoX);
        Canvas.SetTop(ColorInfoBorder, infoY);
    }

    private void UpdateCoordInfo(Point pos)
    {
        CoordInfoText.Text = $"({(int)pos.X},{(int)pos.Y})";

        var coordX = pos.X + 20;
        var coordY = pos.Y + 180;

        if (coordX + 100 > ActualWidth)
            coordX = pos.X - 120;
        if (coordY + 25 > ActualHeight)
            coordY = pos.Y - 80;

        Canvas.SetLeft(CoordInfoBorder, coordX);
        Canvas.SetTop(CoordInfoBorder, coordY);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_selectionComplete ) return;

        _startPoint = e.GetPosition(this);
        _isSelecting = true;

        // 隐藏放大镜
        MagnifierBorder.Visibility = Visibility.Collapsed;
        ColorInfoBorder.Visibility = Visibility.Collapsed;
        CoordInfoBorder.Visibility = Visibility.Collapsed;
        Background = new SolidColorBrush(Color.FromArgb(48, 0, 0, 0));

        // 显示选区边框
        SelectionBorder.Visibility = Visibility.Visible;
    }

    private void UpdateSelection()
    {
        var x = Math.Min(_startPoint.X, _currentPoint.X);
        var y = Math.Min(_startPoint.Y, _currentPoint.Y);
        var width = Math.Abs(_currentPoint.X - _startPoint.X);
        var height = Math.Abs(_currentPoint.Y - _startPoint.Y);

        _selectionRect = new Rect(x, y, width, height);
        this.GetViewModel<ScreenShotComponentViewModel>().SetSelectionRect(_selectionRect);

        // 更新选区边框
        Canvas.SetLeft(SelectionBorder, x);
        Canvas.SetTop(SelectionBorder, y);

        SelectionBorder.Width = width;
        SelectionBorder.Height = height;
        SelectionBorder.Fill = new SolidColorBrush(Colors.Transparent);

        // 更新遮罩（在选区外）
        UpdateOverlay(x, y, width, height);

        // 显示选区尺寸
        ShowSelectionSize(x, y, width, height);
    }


    private void UpdateOverlay(double x, double y, double width, double height)
    {
        OverlayCanvas.Children.Clear();

        // 上方
        var topRect = new Rectangle
        {
            Width = ActualWidth,
            Height = y,
            Fill = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0))
        };
        Canvas.SetLeft(topRect, 0);
        Canvas.SetTop(topRect, 0);
        OverlayCanvas.Children.Add(topRect);

        // 下方
        var bottomRect = new Rectangle
        {
            Width = ActualWidth,
            Height = ActualHeight - y - height,
            Fill = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0))
        };
        Canvas.SetLeft(bottomRect, 0);
        Canvas.SetTop(bottomRect, y + height);
        OverlayCanvas.Children.Add(bottomRect);

        // 左侧
        var leftRect = new Rectangle
        {
            Width = x,
            Height = height,
            Fill = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0))
        };
        Canvas.SetLeft(leftRect, 0);
        Canvas.SetTop(leftRect, y);
        OverlayCanvas.Children.Add(leftRect);

        // 右侧
        var rightRect = new Rectangle
        {
            Width = ActualWidth - x - width,
            Height = height,
            Fill = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0))
        };
        Canvas.SetLeft(rightRect, x + width);
        Canvas.SetTop(rightRect, y);
        OverlayCanvas.Children.Add(rightRect);
    }

    private void ShowSelectionSize(double x, double y, double width, double height)
    {
        SizeInfoText.Text = $"{(int)width} x {(int)height}";
        SizeInfoBorder.Visibility = Visibility.Visible;

        var sizeX = x;
        var sizeY = y - 30;

        if (sizeY < 0)
            sizeY = y + height + 5;

        Canvas.SetLeft(SizeInfoBorder, sizeX);
        Canvas.SetTop(SizeInfoBorder, sizeY);
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;

        _isSelecting = false;
        _selectionComplete = true;

        // 将 DIP 坐标转换为物理像素坐标
        var source = PresentationSource.FromVisual(this);
        var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        var x = (int)(_selectionRect.X * dpiX);
        var y = (int)(_selectionRect.Y * dpiY);
        var width = (int)(_selectionRect.Width * dpiX);
        var height = (int)(_selectionRect.Height * dpiY);

        _screenBitmap = CaptureRegion(x, y, width, height);
        this.GetViewModel<ScreenShotComponentViewModel>().Source = _screenBitmap;
        this.GetViewModel<ScreenShotComponentViewModel>().SetSelectionRect(
            new Rect(0, 0, _screenBitmap.PixelWidth, _screenBitmap.PixelHeight));

        SelectionBorder.Fill = new SolidColorBrush(Colors.Transparent);

        // 显示工具栏
        ShowToolBar();
    }

    private void ShowToolBar()
    {
        ToolBarBorder.Visibility = Visibility.Visible;
        SizeInfoBorder.Visibility = Visibility.Collapsed;

        // 工具栏显示在选区下方
        var toolBarX = _selectionRect.X;
        var toolBarY = _selectionRect.Y + _selectionRect.Height + 5;

        if (toolBarY + 40 > ActualHeight)
            toolBarY = _selectionRect.Y - 45;

        if (toolBarX + 120 > ActualWidth)
            toolBarX = ActualWidth - 125;

        Canvas.SetLeft(ToolBarBorder, toolBarX);
        Canvas.SetTop(ToolBarBorder, toolBarY);
    }

    /// <summary>
    /// 从保存的屏幕像素数据中获取指定物理像素坐标的颜色
    /// </summary>
    public (byte r, byte g, byte b) GetPixelColor(int physicalX, int physicalY)
    {
        if (_screenPixels == null || 
            physicalX < 0 || physicalX >= _screenPixelWidth ||
            physicalY < 0 || physicalY >= _screenPixelHeight)
            return (0, 0, 0);

        var index = physicalY * _screenStride + physicalX * 4;
        var b = _screenPixels[index];
        var g = _screenPixels[index + 1];
        var r = _screenPixels[index + 2];
        return (r, g, b);
    }
    
}
