using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Infrastructure.Services.Services.HomeServices;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Ava.Xioa.Infrastructure.Services.Utils;
using Avalonia;
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
        INavigableMenuServices navigableMenuServices) : base(regionManager,
        mainWindowServices)
    {
        _mainWindowServices = mainWindowServices;
        NavigableMenuServices = navigableMenuServices;
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
        _mainWindowServices.TitleBarVisibilityOnFullScreen = SukiWindow.TitleBarVisibilityMode.Visible;
        base.OnNavigatedTo(navigationContext);
    }
}