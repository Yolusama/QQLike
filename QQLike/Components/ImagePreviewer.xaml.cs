using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class ImagePreviewer
{
    private Point? _drawStartPoint;
    private Polyline? _currentPolyline;
    private Line? _currentLine;
    private Rectangle? _currentRectangle;
    private Ellipse? _currentEllipse;
    private bool _isDrawing;

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

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ImagePreviewerViewModel viewModel)
            viewModel.InitializeCommand.Execute(null);
    }

    // ==================== 画布鼠标事件 ====================

    private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ImagePreviewerViewModel vm) return;
        if (vm.SelectedTool == EditTool.None) return;

        var pos = e.GetPosition(DrawingCanvas);
        _drawStartPoint = pos;
        _isDrawing = true;
        DrawingCanvas.CaptureMouse();

        switch (vm.SelectedTool)
        {
            case EditTool.Rectangle:
                _currentRectangle = new Rectangle
                {
                    Stroke = vm.SelectedColor,
                    StrokeThickness = GetStrokeThickness(vm.SelectedStrokeSize),
                    Fill = Brushes.Transparent,
                };
                SetShapeBounds(_currentRectangle, pos, pos);
                DrawingCanvas.Children.Add(_currentRectangle);
                break;

            case EditTool.Circle:
                _currentEllipse = new Ellipse
                {
                    Stroke = vm.SelectedColor,
                    StrokeThickness = GetStrokeThickness(vm.SelectedStrokeSize),
                    Fill = Brushes.Transparent,
                };
                SetShapeBounds(_currentEllipse, pos, pos);
                DrawingCanvas.Children.Add(_currentEllipse);
                break;

            case EditTool.Arrow:
                _currentLine = new Line
                {
                    Stroke = vm.SelectedColor,
                    StrokeThickness = GetStrokeThickness(vm.SelectedStrokeSize),
                    StrokeEndLineCap = PenLineCap.Triangle,
                    X1 = pos.X,
                    Y1 = pos.Y,
                    X2 = pos.X,
                    Y2 = pos.Y,
                };
                DrawingCanvas.Children.Add(_currentLine);
                break;

            case EditTool.Pencil:
                _currentPolyline = new Polyline
                {
                    Stroke = vm.SelectedColor,
                    StrokeThickness = GetStrokeThickness(vm.SelectedStrokeSize),
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                };
                _currentPolyline.Points.Add(pos);
                DrawingCanvas.Children.Add(_currentPolyline);
                break;

            case EditTool.Eraser:
                TryEraseElement(pos);
                _isDrawing = false;
                DrawingCanvas.ReleaseMouseCapture();
                break;

            case EditTool.Text:
                ShowTextInput(pos, vm);
                _isDrawing = false;
                DrawingCanvas.ReleaseMouseCapture();
                break;
        }
    }

    private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing || DataContext is not ImagePreviewerViewModel vm) return;

        var pos = e.GetPosition(DrawingCanvas);

        switch (vm.SelectedTool)
        {
            case EditTool.Pencil:
                _currentPolyline?.Points.Add(pos);
                break;

            case EditTool.Rectangle:
                if (_currentRectangle != null && _drawStartPoint.HasValue)
                    SetShapeBounds(_currentRectangle, _drawStartPoint.Value, pos);
                break;

            case EditTool.Circle:
                if (_currentEllipse != null && _drawStartPoint.HasValue)
                    SetShapeBounds(_currentEllipse, _drawStartPoint.Value, pos);
                break;

            case EditTool.Arrow:
                if (_currentLine != null)
                {
                    _currentLine.X2 = pos.X;
                    _currentLine.Y2 = pos.Y;
                }
                break;
        }
    }

    private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing || DataContext is not ImagePreviewerViewModel vm) return;

        _isDrawing = false;
        DrawingCanvas.ReleaseMouseCapture();

        var pos = e.GetPosition(DrawingCanvas);
        if (!_drawStartPoint.HasValue) return;

        var start = _drawStartPoint.Value;
        _drawStartPoint = null;

        switch (vm.SelectedTool)
        {
            case EditTool.Rectangle:
                if (_currentRectangle != null)
                    SetShapeBounds(_currentRectangle, start, pos);
                _currentRectangle = null;
                break;

            case EditTool.Circle:
                if (_currentEllipse != null)
                    SetShapeBounds(_currentEllipse, start, pos);
                _currentEllipse = null;
                break;

            case EditTool.Arrow:
                if (_currentLine != null && _currentLine.X1 == _currentLine.X2 && _currentLine.Y1 == _currentLine.Y2)
                {
                    DrawingCanvas.Children.Remove(_currentLine);
                }
                _currentLine = null;
                break;

            case EditTool.Pencil:
                if (_currentPolyline != null && _currentPolyline.Points.Count <= 1)
                {
                    DrawingCanvas.Children.Remove(_currentPolyline);
                }
                _currentPolyline = null;
                break;
        }

    }

    // ==================== 辅助方法 ====================

    private void SetShapeBounds(Shape shape, Point start, Point end)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);

        if (width < 1 && height < 1)
            width = height = 1;

        Canvas.SetLeft(shape, left);
        Canvas.SetTop(shape, top);
        shape.Width = width;
        shape.Height = height;
    }

    private double GetStrokeThickness(StrokeSize size) => size switch
    {
        StrokeSize.Small => 2,
        StrokeSize.Medium => 4,
        StrokeSize.Large => 8,
        _ => 4,
    };

    private void TryEraseElement(Point pos)
    {
        for (int i = DrawingCanvas.Children.Count - 1; i >= 0; i--)
        {
            var child = DrawingCanvas.Children[i];
            if (HitTestElement(child, pos))
            {
                DrawingCanvas.Children.RemoveAt(i);
                break;
            }
        }
    }

    private bool HitTestElement(UIElement element, Point pos)
    {
        if (element is FrameworkElement fe)
        {
            var left = Canvas.GetLeft(fe);
            var top = Canvas.GetTop(fe);
            double width = fe.Width;
            double height = fe.Height;

            if (element is Polyline polyline)
            {
                double thickness = polyline.StrokeThickness + 6;
                foreach (var pt in polyline.Points)
                {
                    if (Math.Abs(pos.X - pt.X) <= thickness && Math.Abs(pos.Y - pt.Y) <= thickness)
                        return true;
                }
                return false;
            }

            if (element is Line line)
            {
                return DistanceToLineSegment(pos, new Point(line.X1, line.Y1), new Point(line.X2, line.Y2)) <= 6;
            }

            if (double.IsNaN(left) || double.IsNaN(top)) return false;
            return pos.X >= left && pos.X <= left + width &&
                   pos.Y >= top && pos.Y <= top + height;
        }

        return false;
    }

    private double DistanceToLineSegment(Point p, Point a, Point b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0) return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));

        double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared));
        double projX = a.X + t * dx;
        double projY = a.Y + t * dy;
        return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
    }

    // ==================== 文字输入 ====================

    private void ShowTextInput(Point position, ImagePreviewerViewModel vm)
    {
        var textBox = new TextBox
        {
            BorderThickness = new Thickness(1),
            BorderBrush = vm.SelectedColor,
            Background = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0)),
            Foreground = vm.SelectedColor,
            CaretBrush = vm.SelectedColor,
            MinWidth = 80,
            MaxWidth = 300,
            AcceptsReturn = false,
            FontSize = 18,
        };
        Canvas.SetLeft(textBox, position.X);
        Canvas.SetTop(textBox, position.Y);
        DrawingCanvas.Children.Add(textBox);

        textBox.Loaded += (_, _) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };

        textBox.KeyDown += (_, ev) =>
        {
            if (ev.Key == Key.Enter)
            {
                FinalizeText(textBox, vm.SelectedColor);
                ev.Handled = true;
            }
            else if (ev.Key == Key.Escape)
            {
                DrawingCanvas.Children.Remove(textBox);
            }
        };

        textBox.LostFocus += (_, _) =>
        {
            FinalizeText(textBox, vm.SelectedColor);
        };
    }

    private void FinalizeText(TextBox textBox, Brush color)
    {
        if (!DrawingCanvas.Children.Contains(textBox)) return;

        var left = Canvas.GetLeft(textBox);
        var top = Canvas.GetTop(textBox);

        DrawingCanvas.Children.Remove(textBox);

        if (!string.IsNullOrWhiteSpace(textBox.Text))
        {
            var textBlock = new TextBlock
            {
                Text = textBox.Text,
                Foreground = color,
                FontSize = textBox.FontSize,
                FontWeight = FontWeights.Bold,
            };
            Canvas.SetLeft(textBlock, left);
            Canvas.SetTop(textBlock, top);
            DrawingCanvas.Children.Add(textBlock);
        }
    }

    // ==================== 公开方法供 ViewModel 调用 ====================

    public void UndoLastAnnotation()
    {
        if (DrawingCanvas.Children.Count > 0)
            DrawingCanvas.Children.RemoveAt(DrawingCanvas.Children.Count - 1);
    }

    public void ClearAnnotations()
    {
        DrawingCanvas.Children.Clear();
    }

    public bool HasAnnotations => DrawingCanvas.Children.Count > 0;

    public Canvas AnnotationCanvas => DrawingCanvas;
    
}
