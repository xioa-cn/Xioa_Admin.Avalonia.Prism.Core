using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Extensions;
using Ava.Xioa.Common.Themes.I18n;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication.Utils;
using AvaloniaApplication.ViewModels;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Controls;

namespace AvaloniaApplication.Views;

[PrismView(ServiceLifetime.Singleton)]
public partial class MainWindow : SukiWindow
{
    private readonly MainWindowViewModel mainWindowViewModel;

    public MainWindow(UserControl userControl, MainWindowViewModel mainWindowViewModel)
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
            var langs = LangUtils.ApplicationLanguages();

            foreach (var lang in langs)
            {
                var menuItem = new MenuItem()
                {
                    Header = lang.Name,
                    Tag = lang.SourceKey
                };
                menuItem.Icon = new MaterialIcon()
                {
                    Kind = MaterialIconKind.Language
                };
                menuItem.Click += (sender, e) => { I18nManager.Instance.ChangeLanguage(lang.Name, lang.SourceKey); };
                this.LangItem.Items.Add(menuItem);
            }
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