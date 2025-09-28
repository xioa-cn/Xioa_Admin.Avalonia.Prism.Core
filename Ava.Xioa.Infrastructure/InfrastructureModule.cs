using Ava.Xioa.Common.Extensions;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace Ava.Xioa.Infrastructure;

public class InfrastructureModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigations(typeof(InfrastructureModule).Assembly);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var regionManager = containerProvider.Resolve<IRegionManager>();

        regionManager.RegisterViewsWithRegion(typeof(InfrastructureModule).Assembly);
    }
}