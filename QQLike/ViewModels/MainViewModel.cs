using System.Collections.ObjectModel;
using MaterialDesignThemes.Wpf;
using QQLike.Domain;
using QQLike.Views;

namespace QQLike.ViewModels;

public class MainViewModel : ViewModelBase<MainView>
{
    public ObservableCollection<MDMenuItem> MenuItems { get; } = [
    
        new MDMenuItem { Title = "消息" },
        new MDMenuItem { Title = "联系人" },
        new MDMenuItem { Title = "动态" },
        new MDMenuItem { Title = "设置" }
    ];
    
    
}
