using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Services;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Controls;

namespace AvaloniaApplication.Views;

[PrismView(ServiceLifetime.Singleton)]
public partial class MainWindow : SukiWindow
{
    public MainWindow(UserControl userControl, MainWindowViewModel viewModel)
    {
        this.DataContext = viewModel;

        InitializeComponent();
        this.WindowContentControl.Content = userControl;
        this.IsEnabled = false;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (this.DataContext is IInitializedable vm)
        {
            vm.Initialized();
        }

        this.IsEnabled = true;
    }
}