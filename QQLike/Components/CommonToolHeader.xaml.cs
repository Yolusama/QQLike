using System.Windows.Controls;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class CommonToolHeader : UserControl
{
    public CommonToolHeader()
    {
        InitializeComponent();
        this.SetViewModel<CommonToolHeaderViewModel,CommonToolHeader>();
    }
}