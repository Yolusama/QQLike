using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class UserControlFactory(IServiceProvider serviceProvider) : IUserControlFactory
{
    
    public T Get<T>() where T : UserControl
    {
        var userControl = serviceProvider.GetRequiredService<T>();
        return userControl;
    }
}