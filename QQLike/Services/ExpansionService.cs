using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Services.Interfaces;
using QQLike.ViewModels;
using StackExchange.Redis;

namespace QQLike.Services;

public static class ExpansionService
{
    /// <summary>
    /// 组件创建无法构造函数注入，故此形式
    /// </summary>
    /// <param name="control">控件</param>
    /// <typeparam name="T">ViewModel类型</typeparam>
    /// <typeparam name="TC">控件类型</typeparam>
    public static void SetViewModel<T,TC>(this UserControl control) where TC : UserControl where T : ViewModelBase<TC>
    {
        var viewModel = App.ServiceProvider.GetRequiredService<T>();
        viewModel.View = (TC)control;
        control.DataContext = viewModel;
    }
    
    public static void SetViewModel<T>(this T window, ViewModelBase<T> viewModel) where T : Window
    {
        window.DataContext = viewModel;
        viewModel.View = window;
    }
    
    public static void AddRedis(this IServiceCollection services,string redisConnectionString)
    {
        var redisConnect = ConnectionMultiplexer.Connect(redisConnectionString);
        services.AddSingleton<IConnectionMultiplexer, ConnectionMultiplexer>(_ => redisConnect);
        services.AddScoped<IRedisCache, RedisCache>();
    }

    public static T GetViewModel<T>(this FrameworkElement element)
    {
        return (T)element.DataContext;
    }
    
    public static void UIDispatch(this ObservableObject viewModelBase,Func<Task> func)
    {
        App.Current.Dispatcher.InvokeAsync(async () => await func());
    }
    
    public static void UIDispatch(this ObservableObject viewModelBase,Action func)
    {
        App.Current.Dispatcher.Invoke(func);
    }

    /// <summary>
    /// 判断 RichTextBox 是否为空
    /// </summary>
    public static bool HasContent(this RichTextBox richTextBox)
    {
        if (richTextBox?.Document == null) return false;

        // 1. 检查文本
        if (!string.IsNullOrWhiteSpace(GetPlainText(richTextBox)))
            return true;

        // 2. 检查是否有 UI 元素（图片、控件等）
        if (HasInlineUIContainer(richTextBox))
            return true;

        return false;
    }

    public static string GetPlainText(this RichTextBox richTextBox)
    {
        var range = new TextRange(
            richTextBox.Document.ContentStart,
            richTextBox.Document.ContentEnd);
        return range.Text.TrimEnd('\r', '\n');
    }
    
    private static bool IsRichTextBoxEmpty(RichTextBox richTextBox)
    {
        // 1. 检查是否有文本
        var range = new TextRange(
            richTextBox.Document.ContentStart,
            richTextBox.Document.ContentEnd);
    
        if (!string.IsNullOrWhiteSpace(range.Text))
            return false;

        // 2. 检查是否有图片
        if (HasImage(richTextBox))
            return false;

        // 3. 检查是否有其他 UI 元素
        if (HasInlineUIContainer(richTextBox))
            return false;

        return true;
    }

    private static bool HasImage(RichTextBox richTextBox)
    {
        foreach (var block in richTextBox.Document.Blocks)
        {
            if (block is Paragraph paragraph)
            {
                foreach (var inline in paragraph.Inlines)
                {
                    if (inline is InlineUIContainer { Child: Image })
                        return true;
                }
            }
        }
        return false;
    }

    private static bool HasInlineUIContainer(RichTextBox richTextBox)
    {
        foreach (var block in richTextBox.Document.Blocks)
        {
            if (block is Paragraph paragraph)
            {
                foreach (var inline in paragraph.Inlines)
                {
                    if (inline is InlineUIContainer)
                        return true;
                }
            }
        }
        return false;
    }
}