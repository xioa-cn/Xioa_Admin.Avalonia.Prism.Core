using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Utils;
using Prism.Events;
using Prism.Navigation.Regions;

namespace AvaloniaApplication.ViewModels;

[PrismViewModel(typeof(MainViewViewModel))]
public class MainViewViewModel : NavigableViewModelObject
{
    public MainViewViewModel(IEventAggregator eventAggregator, IRegionManager regionManager) : base(eventAggregator,
        regionManager)
    {
        var navigationParameters =
            NavigationParametersHelper.TargetNavigationParameters("SplashView", AppRegions.MainRegion);
        base.ExecuteNavigate(navigationParameters);
    }
}