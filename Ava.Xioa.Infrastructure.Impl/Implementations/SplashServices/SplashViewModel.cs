using System.Threading.Tasks;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Services.Services.SplashServices;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Ava.Xioa.Infrastructure.Services.Utils;
using Avalonia;
using Prism.Events;
using Prism.Navigation.Regions;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.SplashServices;

[PrismViewModel(typeof(ISplashServices))]
public class SplashViewModel : NavigableChangeWindowSizeViewModel, ISplashServices, IInitializedAsyncable
{
    private readonly IMainWindowServices _mainWindowServices;

    public SplashViewModel(IEventAggregator eventAggregator, IRegionManager regionManager,
        IMainWindowServices mainWindowServices) : base(eventAggregator, regionManager, mainWindowServices)

    {
        _mainWindowServices = mainWindowServices;
    }

    protected override Size AfterChangeSize { get; } = new Size(650, 550);

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        //base.OnNavigatedTo(navigationContext);
    }

    public async Task InitializedAsync()
    {
        _mainWindowServices.Opacity = 1;
        await Task.Delay(3000);
        var navigationParameters =
            NavigationParametersHelper.TargetNavigationParameters(AppInformation.Instance.SplashIndexView,
                AppRegions.MainRegion);
        ExecuteNavigate(navigationParameters);
    }
}