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
}