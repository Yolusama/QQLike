using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class Index : Window
{
    public Index(IndexViewModel indexViewModel)
    {
        InitializeComponent();
        this.SetViewModel(indexViewModel);
    }

    private void Index_OnLoaded(object sender, RoutedEventArgs e)
    {
        var viewModel = (IndexViewModel)DataContext;
        viewModel.InitLoginSetting();
    }
}