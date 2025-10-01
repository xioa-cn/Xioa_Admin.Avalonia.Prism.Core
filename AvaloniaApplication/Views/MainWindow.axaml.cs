using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Extensions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication.Utils;
using AvaloniaApplication.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Controls;

namespace AvaloniaApplication.Views;

[PrismView(ServiceLifetime.Singleton)]
public partial class MainWindow : SukiWindow
{
    private readonly MainWindowViewModel mainWindowViewModel;
    public MainWindow(UserControl userControl,MainWindowViewModel mainWindowViewModel)
    {
        this.mainWindowViewModel = mainWindowViewModel;
        this.DataContext = mainWindowViewModel;
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        this.WindowState = WindowState.Normal;
        this.Width = 444;
        this.Height = 550;
        this.Loaded += OnLoaded;
        InitializeComponent();
        this.WindowContentControl.Content = userControl;
        
        this.OnceExecutedLoaded(() =>
        {
            LangUtils.ApplicationLanguages();
        });
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        mainWindowViewModel.Initialized();
    }


    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        this.Hide();
    }
}