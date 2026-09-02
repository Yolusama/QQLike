using QQLike.Entity.Enum;
using QQLike.Entity.VO;

namespace QQLike.Functional.Utils;

public static class EnumHelper
{
    private static readonly string[] ImageExtensions =
    [
        ".jpg", // 最流行，有损压缩，适合照片
        ".jpeg", // .jpg 的完整扩展名
        ".png", // 无损压缩，支持透明背景，适合图标/图形
        ".gif", // 支持动画和透明，颜色有限（最多256色）
        ".bmp", // 无压缩，体积巨大，质量最好
        ".tiff", // 高质量，常用于印刷和扫描
        ".webp", // Google推出，支持有损/无损/透明，文件小，现代浏览器广泛支持
        ".svg", // 矢量图，无限放大不失真，适合Logo/图标
        ".ico", // 图标文件，常用于网站favicon
        ".heif", // 苹果主推，H.265编码的同源技术，文件比jpg更小画质更好
        ".heic", // HEIF的苹果专用变种，iPhone照片默认格式
        ".avif", // AV1编码的图片格式，压缩率极高，新兴标准
        ".raw", // 相机原始数据，未经处理，专业摄影使用（不同厂商扩展名不同）
        ".psd"
    ];   // Photoshop源文件

    private static readonly string[] AudioExtensions =
    [
        ".mp3", // 最流行，有损压缩
        ".aac", // MP3继任者，音质更好
        ".m4a", // AAC的另一种扩展名
        ".wav", // 无损，体积巨大
        ".flac", // 无损压缩，音质好体积适中
        ".wma"
    ];  // 微软Windows Media Audio;

    private static readonly string[] VideoExtensions =
    [
        ".mp4",   // 最通用，首选
        ".avi",   // 老牌，体积大
        ".mov",   // 苹果QuickTime
        ".mkv",   // 万能容器，支持多音轨/字幕
        ".webm",  // 专为Web设计
        ".flv",   // 早期流媒体
        ".wmv",   // 微软Windows Media
        ".rmvb",  // RealNetworks，低码率画质好
        ".3gp"    // 移动设备
    ];
    public static List<ValueLabel<int>> ToValueLabels<TEnum>() where TEnum : Enum
    {
        var type = typeof(TEnum);
        var values = Enum.GetValues(type);
        var valueLabels = new List<ValueLabel<int>>();
        foreach (var value in values)
        {
            var intValue = Convert.ToInt32(value);
            var label = Enum.GetName(type, value);
            valueLabels.Add(new ValueLabel<int> { Value = intValue, Label = label });
        }
        return valueLabels;
    }
    
    public static string FileTypeContent(this ChatMessageType type)
    {
        return type switch
        {
            ChatMessageType.Image => "图片",
            ChatMessageType.Video => "视频",
            ChatMessageType.Audio => "音频",
            _ => "文件"
        };
    }

    public static ChatMessageType ToChatMessageType(string suffix)
    {
        if(ImageExtensions.Contains(suffix.ToLower()))
        {
            return ChatMessageType.Image;
        }
        if(VideoExtensions.Contains(suffix.ToLower()))
        {
            return ChatMessageType.Video;
        }
        if(AudioExtensions.Contains(suffix.ToLower()))
        {
            return ChatMessageType.Audio;
        }
        return ChatMessageType.File;
    }
}