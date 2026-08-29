using System.Windows;
using System.Windows.Controls;
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
}