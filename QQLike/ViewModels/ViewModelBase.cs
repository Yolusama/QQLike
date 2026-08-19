using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QQLike.ViewModels;

public abstract class ViewModelBase<T> : ObservableObject where T : FrameworkElement
{
    private T _view = null;
    public T View
    { 
        get => _view; 
        set { _view ??= value; }
    }
    private Window _owner  = null;
    public Window Owner => GetOwner();

    private Window GetOwner()
    {
        if (_owner == null)
        {
            var type = View.GetType();
            if(type.IsAssignableTo(typeof(Window)))
            {
                var window = View as Window;
                _owner = window.Owner;
            }
            else _owner = Window.GetWindow(View);
        }
        return _owner;
    }
    
    
}