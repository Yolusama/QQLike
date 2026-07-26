using System.Windows.Controls;

namespace QQLike.Services.Interfaces;

public interface IUserControlFactory
{
    public T GetUserControl<T>() where T:UserControl;
}