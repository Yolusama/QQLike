using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QQLike.ViewModels;

public abstract class ViewModelBase<T> : ObservableObject where T : FrameworkElement
{
    public T View { get; set; } = default!;
}