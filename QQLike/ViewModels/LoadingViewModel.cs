using System.Windows;
using System.Windows.Media;
using QQLike.Components;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace QQLike.ViewModels;

public partial class LoadingViewModel : ViewModelBase<LoadingComponent>
{
    [ObservableProperty] private bool _loading;
    [ObservableProperty] private string _loadingText = "正在加载";

    public bool Cancelled { get; private set; } = false;
    
    private double _ownerOpacity;

    public void Start(string text = "正在加载")
    {
        LoadingText = text;
        Loading = true;
        
        _ownerOpacity = View.Owner.Opacity;
        View.Owner.Opacity = 0.3;
    }

    public void Complete()
    {
        if(Cancelled)return;
        Loading = false;
        RestoreOwnerStyle();
        View.Close();
    }

    [RelayCommand]
    private void CancelLoading()
    {
        Loading = false;
        Cancelled = true;
        RestoreOwnerStyle();
        View.Close();
    }

    private void RestoreOwnerStyle()
    {
        View.Owner.Opacity = _ownerOpacity;
    }
}