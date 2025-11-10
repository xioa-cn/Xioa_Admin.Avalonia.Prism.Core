using System;
using System.Threading;
using System.Threading.Tasks;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Extensions;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Themes.Dialogs;
using Ava.Xioa.Common.Themes.I18n;
using Ava.Xioa.Common.Themes.Services.Services;
using Ava.Xioa.Common.Themes.Utils;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaApplication.Utils;
using AvaloniaApplication.ViewModels;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Prism.Navigation;
using Prism.Navigation.Regions;
using SukiUI.Controls;
using SukiUI.Dialogs;

namespace AvaloniaApplication.Views;

[PrismView(ServiceLifetime.Singleton)]
public partial class MainWindow : SukiWindow
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    private readonly ICloseDialogService _closeDialogService;

    void NavigationCompleted(NavigationResult result)
    {
    }

    public MainWindow(UserControl userControl, MainWindowViewModel mainWindowViewModel,
        ICloseDialogService closeDialogService)
    {
        this._mainWindowViewModel = mainWindowViewModel;
        _closeDialogService = closeDialogService;

        _closeDialogService.CloseAction += () =>
        {
            mainWindowViewModel.EventAggregator?.GetEvent<ExitApplicationEvent>().Publish(new TokenKeyPubSubEvent<Exit>(
                "ExitApplication",
                new Exit()
                {
                    ExitCode = 0
                }));
        };
        _closeDialogService.LogoutAction += () =>
        {
            GlobalUserInformation.Instance.UserAuthEnum = UserAuthEnum.None;
            mainWindowViewModel.RegionManager.RequestNavigate(AppRegions.MainRegion, "LoginView", NavigationCompleted);
        };
        _closeDialogService.MiniSizeAction += () => { this.Hide(); };

        this.DataContext = mainWindowViewModel;
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        this.WindowState = WindowState.Normal;
        this.Width = 444;
        this.Height = 550;
        this.Loaded += OnLoaded;
        InitializeComponent(attachDevTools: false);
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
        _mainWindowViewModel.Initialized();
    }

    private CloseDialog? _view;
    private SukiDialogBuilder? dialog;

    private object obj = new object();

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (GlobalUserInformation.Instance.IsLogin)
        {
            Task.Run(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    dialog ??= _mainWindowViewModel.DialogManager.CreateVmDialog(_closeDialogService);
                    _view ??= new CloseDialog(_closeDialogService);
                    await dialog.WithTitle("关闭面板")
                        .WithContent(_view).OfType(NotificationType.Warning)
                        .Dismiss().ByClickingBackground().WithAsync().TryShowAsync();
                    await Task.Delay(1000);
                });
            });
        }
        else
        {
            this.Hide();
        }

        e.Cancel = true;
    }
}