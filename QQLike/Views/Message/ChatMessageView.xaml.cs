using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaterialDesignThemes.Wpf;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Views.Message;

public partial class ChatMessageView : UserControl
{
    private ChatMessageViewModel ViewModel => this.GetViewModel<ChatMessageViewModel>();
    public ChatMessageView()
    {
        InitializeComponent();
        this.SetViewModel<ChatMessageViewModel,ChatMessageView>();
        
        // 添加粘贴命令绑定
        var pasteCommandBinding = new CommandBinding(ApplicationCommands.Paste, OnPasteExecuted);
        ContentWriteTo.CommandBindings.Add(pasteCommandBinding);
    }

    private void ChatMessageView_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
            ViewModel.LoadDataCommand.Execute(null);
    }

    /// <summary>
    /// 处理粘贴命令
    /// </summary>
    private void OnPasteExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var dataObject = Clipboard.GetDataObject();
        if (dataObject == null) return;
        
        // 检查剪贴板中是否有图片
        if (dataObject.GetDataPresent(DataFormats.Bitmap))
        {
            e.Handled = true;
            
            var bitmap = dataObject.GetData(DataFormats.Bitmap) as BitmapSource;
            if (bitmap != null)
            {
                InsertImageToRichTextBox(bitmap);
            }
        }
        // 检查是否有文件（如从文件管理器复制的图片文件）
        else if (dataObject.GetDataPresent(DataFormats.FileDrop))
        {
            var files = dataObject.GetData(DataFormats.FileDrop) as string[];
            if (files is { Length: > 0 })
            {
                var file = files[0];
                var ext = Path.GetExtension(file).ToLower();
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".ico" };
                
                if (Array.Exists(imageExtensions, x => x == ext))
                {
                    e.Handled = true;
                    
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(file, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze(); // 冻结以便跨线程使用
                    
                    InsertImageToRichTextBox(bitmap);
                }
            }
        }
        else
        {
            // 如果不是图片，执行默认粘贴行为（粘贴文本）
            ContentWriteTo.Paste();
        }
    }

    /// <summary>
    /// 将图片插入到 RichTextBox 中
    /// </summary>
    private void InsertImageToRichTextBox(BitmapSource bitmap)
    {
        // 生成唯一文件名保存图片
        var tempPath = Path.Combine(Path.GetTempPath(), "QQLike", "PasteImages");
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }
        
        var fileName = $"{Guid.NewGuid()}.png";
        var filePath = Path.Combine(tempPath, fileName);
        
        // 保存图片到临时文件
        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(fs);
        }
        
        // 创建 BitmapImage 用于显示
        var displayBitmap = new BitmapImage();
        displayBitmap.BeginInit();
        displayBitmap.UriSource = new Uri(filePath, UriKind.Absolute);
        displayBitmap.CacheOption = BitmapCacheOption.OnLoad;
        displayBitmap.EndInit();
        displayBitmap.Freeze();
        
        // 创建 Image 元素 - 设置明确的宽高
        var image = new Image
        {
            Source = displayBitmap,
            Width = Math.Min(displayBitmap.PixelWidth, 200),
            Height = Math.Min(displayBitmap.PixelHeight, 200),
            MaxWidth = 200,
            MaxHeight = 200,
            Stretch = Stretch.Uniform
        };
        
        // 创建 InlineUIContainer 包裹图片
        var container = new InlineUIContainer(image)
        {
            BaselineAlignment = BaselineAlignment.Center
        };
        
        // 在当前光标位置插入图片
        var caretPosition = ContentWriteTo.CaretPosition;
        var paragraph = caretPosition.Paragraph;
        
        if (paragraph != null)
        {
            // 如果段落有内容，先添加换行
            if (paragraph.Inlines.Count > 0)
            {
                paragraph.Inlines.Add(new LineBreak());
            }
            
            // 插入图片
            paragraph.Inlines.Add(container);
            
            // 在图片后添加换行，方便继续输入
            paragraph.Inlines.Add(new LineBreak());
        }
        
        // 更新布局
        ContentWriteTo.UpdateLayout();
        ContentWriteTo.Focus();
    }

    private void ToggleMediaPlay(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle)
            return;

        var presenter = FindAncestor<ContentPresenter>(toggle);
        var media = presenter is null ? null : FindDescendant<MediaElement>(presenter);
        if (media is null || media.Source is null)
            return;

        if (toggle.IsChecked == true)
            media.Play();
        else
            media.Pause();

        UpdatePlayIcon(toggle);
    }

    private static void UpdatePlayIcon(ToggleButton toggle)
    {
        if (toggle.Content is PackIcon icon)
            icon.Kind = toggle.IsChecked == true ? PackIconKind.Pause : PackIconKind.Play;
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;

            var result = FindDescendant<T>(child);
            if (result is not null)
                return result;
        }

        return null;
    }
}