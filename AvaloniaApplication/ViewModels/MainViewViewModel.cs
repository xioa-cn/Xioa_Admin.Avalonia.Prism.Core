using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Utils;
using Prism.Events;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace AvaloniaApplication.ViewModels;

[PrismViewModel(typeof(MainViewViewModel))]
public class MainViewViewModel : NavigableViewModelObject
{
    public ISukiToastManager ToastManager { get; }
    public ISukiDialogManager DialogManager { get; }

    public MainViewViewModel(IEventAggregator eventAggregator, IRegionManager regionManager,
        ISukiToastManager toastManager, ISukiDialogManager dialogManager) : base(eventAggregator,
        regionManager)
    {
        ToastManager = toastManager;
        DialogManager = dialogManager;
        var navigationParameters =
            NavigationParametersHelper.TargetNavigationParameters("SplashView", AppRegions.MainRegion);
        base.ExecuteNavigate(navigationParameters);
    }
}