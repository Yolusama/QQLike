using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class WindowFactory(IServiceProvider serviceProvider) : IWindowFactory
{
    public T GetWindow<T>(Window? owner = null) where T : Window
    {
        var window = serviceProvider.GetRequiredService<T>();
        if(owner != null)
            window.Owner = owner;
        return window;
    }

    public void GetAndShowWindow<T>(Window? owner = null) where T : Window
    {
        var window = GetWindow<T>(owner);
        window.Show();
    }
}