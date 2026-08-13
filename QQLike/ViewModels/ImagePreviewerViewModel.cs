using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using QQLike.Components;
using QQLike.Entity.Enum;
using Tesseract;
using Rect = System.Windows.Rect;

namespace QQLike.ViewModels;

public partial class ImagePreviewerViewModel(TesseractEngine ocrEngine) : ViewModelBase<ImagePreviewer>
{
    [ObservableProperty]
    private ImageSource? _imageSource;
    [ObservableProperty]
    private double _scale;
    [ObservableProperty]
    private double _rotationAngle;
    [ObservableProperty]
    private string _scaleText = "100%";
    [ObservableProperty]
    private bool _isEditMode;
    [ObservableProperty]
    private EditTool _selectedTool = EditTool.None;
    [ObservableProperty]
    private bool _isBottomToolbarVisible = true;
    [ObservableProperty]
    private Brush _selectedColor = new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0x00));
    [ObservableProperty]
    private StrokeSize _selectedStrokeSize = StrokeSize.Medium;

    [RelayCommand]
    private void Initialize()
    {
        Scale = 1.0;
        ScaleText = $"{Scale * 100}%";
    }

    [RelayCommand]
    private void ZoomIn()
    {
        Scale = Math.Round(Math.Min(Scale * 1.2, 10), 0);
        ScaleText = $"{Scale * 100}%";
    }

    [RelayCommand]
    private void ZoomOut()
    {
        Scale = Math.Round(Math.Max(Scale / 1.2, 0.1), 0);
        ScaleText = $"{Scale * 100}%";
    }

    [RelayCommand]
    private void BackToActualSize()
    {
        Scale = 1.0;
        ScaleText = $"{Scale * 100}%";
    }

    [RelayCommand]
    private void Rotate()
    {
        RotationAngle = (RotationAngle + 90) % 360;
    }

    [RelayCommand]
    private void Edit()
    {
        IsEditMode = true;
        IsBottomToolbarVisible = false;
        // 进入编辑模式时重置缩放，保证标注坐标与图片对齐
        Scale = 1.0;
        ScaleText = "100%";
        RotationAngle = 0;
    }

    [RelayCommand]
    private void SelectRectangle() => SelectedTool = EditTool.Rectangle;

    [RelayCommand]
    private void SelectCircle() => SelectedTool = EditTool.Circle;

    [RelayCommand]
    private void SelectArrow() => SelectedTool = EditTool.Arrow;

    [RelayCommand]
    private void SelectPencil() => SelectedTool = EditTool.Pencil;

    [RelayCommand]
    private void SelectEraser() => SelectedTool = EditTool.Eraser;

    [RelayCommand]
    private void SelectText() => SelectedTool = EditTool.Text;

    [RelayCommand]
    private void SelectColor(string colorHex)
    {
        var color = (Color)ColorConverter.ConvertFromString(colorHex);
        SelectedColor = new SolidColorBrush(color);
    }

    [RelayCommand]
    private void ChooseStrokeSize(string brokeSizeName)
    {
        var size = Enum.Parse<StrokeSize>(brokeSizeName);
        SelectedStrokeSize = size;
    }

    [RelayCommand]
    private void ChooseColor(string color)
    {
        switch (color)
        {
            case "Red":
                SelectedColor = new SolidColorBrush(Colors.Red);
                break;
            case "Yellow":
                SelectedColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
                break;
            case "Green":
                SelectedColor = new SolidColorBrush(Color.FromArgb(255, 0, 221, 102));
                break;
            case "Blue":
                SelectedColor = new SolidColorBrush(Color.FromArgb(255, 0, 153, 255));
                break;
            case "White":
                SelectedColor = new SolidColorBrush(Colors.White);
                break;
            case "Gray":
                SelectedColor = new SolidColorBrush(Color.FromArgb(255, 169, 169, 169));
                break;
            case "Black":
                SelectedColor = new SolidColorBrush(Colors.Black);
                break;
        }
    }

    /// <summary>
    /// 撤销：移除画布上最后一个标注
    /// </summary>
    [RelayCommand]
    private void Undo()
    {
        View.UndoLastAnnotation();
    }

    /// <summary>
    /// 保存编辑结果到文件
    /// </summary>
    [RelayCommand]
    private void SaveEdit()
    {
        try
        {
            var bitmap = RenderAnnotatedImage();
            if (bitmap == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg",
            };

            if (dialog.ShowDialog() == true)
            {
                if(string.IsNullOrWhiteSpace(dialog.FileName))return;
                BitmapEncoder encoder = dialog.FilterIndex switch
                {
                    2 => new JpegBitmapEncoder(),
                    _ => new PngBitmapEncoder(),
                };
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var stream = new FileStream(dialog.FileName, FileMode.Create);
                encoder.Save(stream);
                MessageComponent.ShowMessage(View,"保存成功",MessageType.Success);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(View, $"保存失败,出现异常：{e.Message}", MessageType.Error);
            throw;
        }
    }

    /// <summary>
    /// 取消编辑：丢弃所有标注，退出编辑模式
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        View.ClearAnnotations();
        ExitEditMode();
    }

    /// <summary>
    /// 确认编辑：将带标注的图片保存到系统剪切板，然后退出编辑模式
    /// </summary>
    [RelayCommand]
    private void ConfirmEdit()
    {
        try
        {
            var bitmap = RenderAnnotatedImage();
            if (bitmap != null)
            {
                Clipboard.SetImage(bitmap);
            }
            View.ClearAnnotations();
            ExitEditMode();
            MessageComponent.ShowMessage(View, "已将编辑结果复制到系统剪切板");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(View, $"保存失败,出现异常：{e.Message}", MessageType.Error);
        }
        
    }

    /// <summary>
    /// 提取图片文字（Tesseract OCR）
    /// </summary>
    [RelayCommand]
    private void TakeText()
    {
        if (ImageSource is not BitmapSource bitmapSource)
        {
            MessageComponent.ShowMessage(View, "无法识别：图片源无效", MessageType.Warning);
            return;
        }

        try
        {
            // 1. BitmapSource → PNG 字节数组
            using var ms = new MemoryStream();
            var pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            pngEncoder.Save(ms);
            var pngBytes = ms.ToArray();

            // 2. PNG → Tesseract Pix
            using var pix = Pix.LoadFromMemory(pngBytes);

            // 3. OCR 识别
            using var page = ocrEngine.Process(pix);
            var text = page.GetText()?.Trim();

            if (!string.IsNullOrWhiteSpace(text))
            {
                Clipboard.SetText(text);
                MessageComponent.ShowMessage(View,
                    $"已提取文字并复制到剪贴板:\n{text}", MessageType.Success);
            }
            else
                MessageComponent.ShowMessage(View, "未识别到文字内容", MessageType.Warning);
        }
        catch (Exception ex)
        {
            MessageComponent.ShowMessage(View, $"文字提取失败: {ex.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    private void Share() { }

    [RelayCommand]
    private void Save()
    {
        // 如果有标注内容，渲染带标注的图片；否则保存原图
        try
        {
            var imageSource = View.HasAnnotations ? RenderAnnotatedImage() : ImageSource as BitmapSource;
            if (imageSource == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg",
                FileName = "image.png",
                Title = "保存图片"
            };

            if (dialog.ShowDialog() == true)
            {
                BitmapEncoder encoder = dialog.FilterIndex switch
                {
                    2 => new JpegBitmapEncoder(),
                    _ => new PngBitmapEncoder(),
                };
                encoder.Frames.Add(BitmapFrame.Create(imageSource));
                using var stream = new FileStream(dialog.FileName, FileMode.Create);
                encoder.Save(stream);
                MessageComponent.ShowMessage(View, "保存成功",MessageType.Success);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MessageComponent.ShowMessage(View, $"保存失败,出现异常：{e.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    private void More() { }

    // ==================== 渲染辅助方法 ====================

    /// <summary>
    /// 将原图 + Canvas 标注渲染为一张位图（原图分辨率）
    /// </summary>
    private BitmapSource? RenderAnnotatedImage()
    {
        if (ImageSource is not BitmapSource imageSource) return null;

        int w = imageSource.PixelWidth;
        int h = imageSource.PixelHeight;

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            // 1. 绘制原图
            dc.DrawImage(imageSource, new Rect(0, 0, w, h));

            // 2. 绘制 Canvas 上的标注（Canvas 坐标为 DIP，需按图片 DPI 缩放到像素坐标）
            var canvas = View.AnnotationCanvas;
            var scaleX = imageSource.DpiX / 96.0;
            var scaleY = imageSource.DpiY / 96.0;
            dc.PushTransform(new ScaleTransform(scaleX, scaleY));
            foreach (UIElement child in canvas.Children)
            {
                DrawAnnotation(dc, child);
            }
            dc.Pop();

        }

        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        return rtb;
    }

    private void DrawAnnotation(DrawingContext dc, UIElement child)
    {
        if (child is Rectangle rect)
        {
            var left = Canvas.GetLeft(rect);
            var top = Canvas.GetTop(rect);
            if (double.IsNaN(left) || double.IsNaN(top)) return;
            dc.DrawRectangle(
                rect.Fill ?? Brushes.Transparent,
                new Pen(rect.Stroke, rect.StrokeThickness),
                new Rect(left, top, rect.Width, rect.Height));
        }
        else if (child is Ellipse ellipse)
        {
            var left = Canvas.GetLeft(ellipse);
            var top = Canvas.GetTop(ellipse);
            if (double.IsNaN(left) || double.IsNaN(top)) return;
            dc.DrawEllipse(
                ellipse.Fill ?? Brushes.Transparent,
                new Pen(ellipse.Stroke, ellipse.StrokeThickness),
                new Point(left + ellipse.Width / 2, top + ellipse.Height / 2),
                ellipse.Width / 2, ellipse.Height / 2);
        }
        else if (child is Polyline polyline && polyline.Points.Count >= 2)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(polyline.Points[0], false, false);
                ctx.PolyLineTo(polyline.Points.Skip(1).ToList(), true, false);
            }
            geometry.Freeze();
            dc.DrawGeometry(null,
                new Pen(polyline.Stroke, polyline.StrokeThickness)
                {
                    LineJoin = PenLineJoin.Round,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                },
                geometry);
        }
        else if (child is Line line)
        {
            dc.DrawLine(
                new Pen(line.Stroke, line.StrokeThickness)
                {
                    EndLineCap = line.StrokeEndLineCap,
                },
                new Point(line.X1, line.Y1),
                new Point(line.X2, line.Y2));
        }
        else if (child is TextBlock tb)
        {
            var left = Canvas.GetLeft(tb);
            var top = Canvas.GetTop(tb);
            if (double.IsNaN(left) || double.IsNaN(top)) return;
            var formattedText = new FormattedText(
                tb.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch),
                tb.FontSize,
                tb.Foreground,
                96);
            dc.DrawText(formattedText, new Point(left, top));
        }
    }
    

    private void ExitEditMode()
    {
        IsEditMode = false;
        IsBottomToolbarVisible = true;
        SelectedTool = EditTool.None;
    }
}

public enum EditTool
{
    None,
    Rectangle,
    Circle,
    Arrow,
    Pencil,
    Eraser,
    Text
}

public enum StrokeSize
{
    Small,
    Medium,
    Large
}
