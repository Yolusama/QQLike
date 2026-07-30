using System.Windows.Controls;

namespace QQLike.Services.Interfaces;

public interface IUserControlFactory
{
    public T Get<T>() where T:UserControl;
}