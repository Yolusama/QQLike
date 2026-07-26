using System.Windows;

namespace QQLike.Services.Interfaces;

public interface IWindowFactory
{
    public T GetWindow<T>(Window? owner = null) where T : Window;
    public void GetAndShowWindow<T>(Window? owner=null) where T : Window;
}