using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Extensions;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Themes.Dialogs;
using Ava.Xioa.Common.Themes.I18n;
using Ava.Xioa.Common.Themes.Services.Services;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
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

        _closeDialogService.CloseAction += () => { };
        _closeDialogService.LogoutAction += () =>
        {
            GlobalUserInformation.Instance.UserAuthEnum = UserAuthEnum.None;
            mainWindowViewModel.RegionManager.RequestNavigate(AppRegions.MainRegion, "LoginView", NavigationCompleted);
        };
        _closeDialogService.MiniSizeAction += this.Hide;

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
        //mainWindowViewModel.Initialized();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (GlobalUserInformation.Instance.IsLogin)
        {
            var dialog = _mainWindowViewModel.DialogManager.CreateDialog();
            _closeDialogService.SetDialog(dialog.Dialog);
            var view = new CloseDialog()
            {
                DataContext = _closeDialogService,
            };
            dialog.WithTitle("关闭面板")
                .WithContent(view).OfType(NotificationType.Warning)
                .TryShow();
        }
        else
        {
            this.Hide();
        }

        e.Cancel = true;
    }
}