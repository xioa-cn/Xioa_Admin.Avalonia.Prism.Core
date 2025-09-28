﻿using System.Threading.Tasks;
using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Services.Services.SplashServices;
using Prism.Events;
using Prism.Navigation.Regions;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.SplashServices;

[PrismVm(typeof(ISplashServices))]
public class SplashViewModel : NavigableViewModelObject, ISplashServices, IInitializedAsyncable
{
    public SplashViewModel(IEventAggregator eventAggregator, IRegionManager regionManager) : base(eventAggregator,
        regionManager)
    {
    }

    public async Task InitializedAsync()
    {
        await Task.Delay(3000);
        var navigationParameters =
            NavigationParametersHelper.TargetNavigationParameters("HomeView", AppRegions.MainRegion);
        ExecuteNavigate(navigationParameters);
    }
}