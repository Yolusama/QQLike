using System.Windows.Controls;
using QQLike.Functional.Instructure;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class AppHeader : UserControl
{
    public AppHeader()
    {
        InitializeComponent();
        this.SetViewModel<AppHeaderViewModel,AppHeader>();
        
    }
}