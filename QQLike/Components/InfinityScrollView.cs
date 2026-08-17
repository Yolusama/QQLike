using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QQLike.Components;

/// <summary>
/// 滚动加载方向。
/// </summary>
public enum ScrollLoadDirection
{
    /// <summary>向下滚动到底部时触发加载（默认）。</summary>
    Down,

    /// <summary>向上滚动到顶部时触发加载，初始定位在底部。</summary>
    Up
}

/// <summary>
/// 可复用的无限滚动容器，滚到底部时自动触发 LoadMoreCommand 加载更多数据。
/// </summary>
public class InfinityScrollView : ScrollViewer
{
    public InfinityScrollView()
    {
        ScrollChanged += OnScrollChanged;
        Loaded += OnLoaded;
    }

    /// <summary>滚动加载方向，默认向下。</summary>
    public ScrollLoadDirection Direction
    {
        get => (ScrollLoadDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.Register(nameof(Direction), typeof(ScrollLoadDirection), typeof(InfinityScrollView),
            new PropertyMetadata(ScrollLoadDirection.Down));

    /// <summary>距离底部多少像素时触发加载，默认 30。</summary>
    public double BottomThreshold
    {
        get => (double)GetValue(BottomThresholdProperty);
        set => SetValue(BottomThresholdProperty, value);
    }

    public static readonly DependencyProperty BottomThresholdProperty =
        DependencyProperty.Register(nameof(BottomThreshold), typeof(double), typeof(InfinityScrollView),
            new PropertyMetadata(30.0));

    /// <summary>向上模式时，距离顶部多少像素时触发加载，默认 30。</summary>
    public double TopThreshold
    {
        get => (double)GetValue(TopThresholdProperty);
        set => SetValue(TopThresholdProperty, value);
    }

    public static readonly DependencyProperty TopThresholdProperty =
        DependencyProperty.Register(nameof(TopThreshold), typeof(double), typeof(InfinityScrollView),
            new PropertyMetadata(30.0));

    /// <summary>滚到底部时执行的命令。</summary>
    public ICommand? LoadMoreCommand
    {
        get => (ICommand?)GetValue(LoadMoreCommandProperty);
        set => SetValue(LoadMoreCommandProperty, value);
    }

    public static readonly DependencyProperty LoadMoreCommandProperty =
        DependencyProperty.Register(nameof(LoadMoreCommand), typeof(ICommand), typeof(InfinityScrollView),
            new PropertyMetadata(null));

    /// <summary>是否正在加载中，为 true 时不会重复触发加载。</summary>
    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(InfinityScrollView),
            new PropertyMetadata(false));

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 向上模式默认聚焦最下面，方便用户往上滚动加载历史内容
        if (Direction == ScrollLoadDirection.Up)
            ScrollToBottom();
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // 忽略非用户滚动的变化（如内容尺寸变化导致的偏移变化）
        if (Math.Abs(e.VerticalChange) < 0.1)
            return;

        if (IsLoading)
            return;

        if (LoadMoreCommand is null)
            return;

        if (Direction == ScrollLoadDirection.Up)
        {
            // 向上滚动且到达顶部阈值时触发
            if (e.VerticalChange < 0 && VerticalOffset <= TopThreshold)
                ExecuteLoadMore();
        }
        else
        {
            // 滚到底部时触发
            if (VerticalOffset >= ScrollableHeight - BottomThreshold)
                ExecuteLoadMore();
        }
    }

    private void ExecuteLoadMore()
    {
        var command = LoadMoreCommand;
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }
}
