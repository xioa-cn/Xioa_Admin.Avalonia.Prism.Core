using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Infrastructure.Services.Services.HomeServices;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Ava.Xioa.Infrastructure.Services.Utils;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using Prism.Navigation.Regions;
using SukiUI.Controls;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.HomeServices;

[PrismViewModel(typeof(IHomeServices), ServiceLifetime.Singleton)]
public class HomeViewModel : NavigableChangeWindowSizeViewModel, IHomeServices
{
    private readonly IMainWindowServices _mainWindowServices;

    public IMainWindowServices MainWindowServices => _mainWindowServices;
    public INavigableMenuServices NavigableMenuServices { get; }

    public HomeViewModel(IRegionManager regionManager, IMainWindowServices mainWindowServices,
        INavigableMenuServices navigableMenuServices,HotKeyServices hotKeyServices,ToastsService toastsService) : base(regionManager,
        mainWindowServices)
    {
        _mainWindowServices = mainWindowServices;
        NavigableMenuServices = navigableMenuServices;
        
        hotKeyServices.SetPageHotKey(new KeyGesture(Key.F, KeyModifiers.Control), () =>
        {
            toastsService.ShowToast(NotificationType.Information,"title","HotKey F + Control");
        },"ShowF");
    }

    protected override Size AfterChangeSize { get; } = new Size(1536, 808);

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
}