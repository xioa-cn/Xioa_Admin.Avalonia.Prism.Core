using Ava.Xioa.Common.Extensions;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace Ava.Xioa.Common.Modularity;

public abstract class PrismAutoModule<T> : IModule
{
    public virtual void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigations(typeof(T).Assembly);
    }

    public virtual void OnInitialized(IContainerProvider containerProvider)
    {
        var regionManager = containerProvider.Resolve<IRegionManager>();

        regionManager.RegisterViewsWithRegion(typeof(T).Assembly);
    }
}