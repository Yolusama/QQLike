using System.Windows;
using System.Windows.Controls;
using QQLike.Domain;
using QQLike.Entity.Enum;

namespace QQLike.Views.Templates;

/// <summary>
/// 根据消息类型选择气泡内容模板。Head / Heartbeat 属于其他模式，这里统一回退到文本模板。
/// </summary>
public class ChatMessageBodyTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }
    public DataTemplate? ImageTemplate { get; set; }
    public DataTemplate? AudioTemplate { get; set; }
    public DataTemplate? VideoTemplate { get; set; }
    public DataTemplate? FileTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not ChatMessageItem message)
            return TextTemplate;

        return message.MessageType switch
        {
            ChatMessageType.Image => ImageTemplate,
            ChatMessageType.Audio => AudioTemplate,
            ChatMessageType.Video => VideoTemplate,
            ChatMessageType.File => FileTemplate,
            _ => TextTemplate
        };
    }
}

