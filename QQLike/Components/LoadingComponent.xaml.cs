using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Components;

public partial class LoadingComponent : Window
{
    public LoadingComponent(LoadingViewModel viewModel)
    {
        InitializeComponent();
        this.SetViewModel(viewModel);
    }

    public LoadingViewModel ShowLoading(Window owner,string text)
    {
        var viewModel = DataContext as LoadingViewModel;
        Owner = owner;
        Show();
        viewModel.Start(text);
        return viewModel;
    }

    public static LoadingViewModel Loading(Window owner, string text)
    {
        var loading = App.ServiceProvider.GetRequiredService<LoadingComponent>();
        var viewModel = (LoadingViewModel)loading.DataContext;
        viewModel.Loading = true;
        loading.Show();
        loading.Owner = owner;
        viewModel.Start(text);
        return viewModel;
    }
}