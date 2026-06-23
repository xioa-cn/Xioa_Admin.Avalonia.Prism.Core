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
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Connectlayer.Global;
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
            mainWindowViewModel.RegionManager.RequestNavigate(AppRegions.MainRegion, AvaRouter.LoginView,
                NavigationCompleted);
        };
        _closeDialogService.MiniSizeAction += () => { this.Hide(); };

        this.DataContext = mainWindowViewModel;
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        // this.WindowState = WindowState.Normal;
        InitializeComponent();
        this.WindowContentControl.Content = userControl;

        // this.OnceExecutedLoaded(() =>
        // {
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
        // });
    }

    private CloseDialog? _view;
    private SukiDialogBuilder? _dialog;

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        if (GlobalUserInformation.Instance.IsLogin)
        {
            if (_dialog is not null && _dialog.Dialog.CanDismissWithBackgroundClick)
            {
                return;
            }


            Dispatcher.UIThread.Invoke(() =>
            {
                _mainWindowViewModel.DialogManager.DismissDialog();
                _dialog = _mainWindowViewModel.DialogManager.CreateVmDialog(_closeDialogService);
                _view ??= new CloseDialog();
                _view.DataContext = _closeDialogService;
                _dialog.WithTitle("关闭面板")
                    .WithContent(_view).OfType(NotificationType.Warning)
                    .Dismiss().ByClickingBackground().WithAsync().TryShow();

                _dialog = null;
            });
        }
        else
        {
            this.Hide();
        }
    }
}