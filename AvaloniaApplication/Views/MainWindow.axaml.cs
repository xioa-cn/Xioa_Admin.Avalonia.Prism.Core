using Ava.Xioa.Common.Attributes;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Controls;

namespace AvaloniaApplication.Views;

[PrismView(ServiceLifetime.Singleton)]
public partial class MainWindow : SukiWindow
{
    public MainWindow(UserControl userControl)
    {
        InitializeComponent();
        this.WindowContentControl.Content = userControl;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (this.DataContext is MainWindowViewModel vm)
        {
            vm.AnimationsEnabled = true;
        }
        this.BackgroundAnimationEnabled = true;
    }
}