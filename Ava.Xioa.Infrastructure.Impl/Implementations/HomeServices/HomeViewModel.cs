using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Connectlayer.Global;
using Ava.Xioa.Infrastructure.Services.Services.HomeServices;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Ava.Xioa.Infrastructure.Services.Utils;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using Prism.Core.Mvvm;
using Prism.Events;
using Prism.Navigation;
using Prism.Navigation.Regions;
using SukiUI.Controls;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.HomeServices;

[PrismViewModel(typeof(IHomeServices), ServiceLifetime.Singleton)]
public class HomeViewModel : NavigableChangeWindowSizeViewModel, IHomeServices, IVmLoaded
{
    private readonly IMainWindowServices _mainWindowServices;

    public IMainWindowServices MainWindowServices => _mainWindowServices;
    public INavigableMenuServices NavigableMenuServices { get; }

    private readonly OnceLoaded _onLoaded;

    public HomeViewModel(IRegionManager regionManager, IEventAggregator eventAggregator,
        IMainWindowServices mainWindowServices,
        INavigableMenuServices navigableMenuServices, HotKeyServices hotKeyServices,
        IToastsService toastsService) : base(eventAggregator, regionManager,
        mainWindowServices)
    {
        _mainWindowServices = mainWindowServices;
        NavigableMenuServices = navigableMenuServices;

        hotKeyServices.SetPageHotKey(new KeyGesture(Key.Escape),
            () =>
            {
                if (mainWindowServices.WindowState == WindowState.FullScreen)
                {
                    mainWindowServices.WindowState = WindowState.Normal;
                }
            },
            "Close_Full_Screen");

        hotKeyServices.SetPageHotKey(new KeyGesture(Key.F, KeyModifiers.Control),
            () =>
            {
                if (mainWindowServices.WindowState != WindowState.FullScreen)
                {
                    mainWindowServices.WindowState = WindowState.FullScreen;
                }
            },
            "Open_Full_Screen");

        _onLoaded = new OnceLoaded();

        _onLoaded.SetOnLoaded(() =>
        {
            toastsService.ShowToast(
                NotificationType.Success, 
                "Hello",
                GetDayText.ApplicationSayHello(GlobalUserInformation.Instance.UserName!)
            );
            ExecuteNavigate(
                NavigationParametersHelper.TargetNavigationParameters(AvaRouter.HomeWelcome,
                    AppRegions.HomeRegion));
        });
    }

    protected override Size? AfterChangeSize { get; } = new Size(1536, 808);

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        _mainWindowServices.CanFullScreen = true;
        _mainWindowServices.CanMinimize = true;
        _mainWindowServices.CanMaximize = true;
        _mainWindowServices.IsTitleBarVisible = true;
        _mainWindowServices.CanPin = true;
        _mainWindowServices.CanResize = true;
        _mainWindowServices.ShowTitlebarBackground = true;
        _mainWindowServices.ShowBottomBorder = true;
        _mainWindowServices.CanMove = true;
        _mainWindowServices.IsMenuVisible = true;
        _mainWindowServices.TitleBarVisibilityOnFullScreen = SukiWindow.TitleBarVisibilityMode.Visible;
        base.OnNavigatedTo(navigationContext);
    }

    public void Load()
    {
        _onLoaded.Load();
    }

    public void Unload()
    {
        _onLoaded.Unload();
    }
}