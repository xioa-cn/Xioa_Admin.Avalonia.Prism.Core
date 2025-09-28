using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Utils;
using Prism.Navigation.Regions;

namespace AvaloniaApplication.ViewModels;

[PrismVm(typeof(MainViewViewModel))]
public class MainViewViewModel : NavigableViewModelObject
{
    public MainViewViewModel(IRegionManager regionManager) : base(regionManager)
    {
        var navigationParameters =
            NavigationParametersHelper.TargetNavigationParameters("SplashView", AppRegions.MainRegion);
        base.ExecuteNavigate(navigationParameters);
    }
}