﻿using Ava.Xioa.Infrastructure.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace Ava.Xioa.Infrastructure;

public class InfrastructureModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation(typeof(ThemesManager), "ThemesManager");
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var regionManager = containerProvider.Resolve<IRegionManager>();

        regionManager.RegisterViewWithRegion("MainRegion", typeof(ThemesManager));
    }
}