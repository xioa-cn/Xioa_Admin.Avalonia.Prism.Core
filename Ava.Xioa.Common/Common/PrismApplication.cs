using Ava.Xioa.Common.Common.DI;
using Ava.Xioa.Common.Extensions;
using Prism;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;

namespace Ava.Xioa.Common.Common;

public abstract class PrismApplication : PrismApplicationBase
{
    private IContainerExtension? _containerExtension;

    protected override IContainerExtension CreateContainerExtension()
    {
        var containerRegister = new MSDIContainerRegistry();
        RegisterServices(containerRegister);

        // containerRegister.ServiceCollection.AddPrismServices()
        //     .AddPrismVms().AddPrismViews();

        containerRegister.RegisterSingleton(typeof(IContainerExtension), obj =>
            _containerExtension);

        _containerExtension = containerRegister.BuildServiceProvider();

        return _containerExtension;
    }


    protected virtual void RegisterServices(IContainerRegistry containerRegistry)
    {
        //containerRegistry.RegisterSingleton<RegionAdapterMappings>();
        //containerRegistry.RegisterSingleton<IRegionManager, RegionManager>();
        //containerRegistry.RegisterSingleton<IRegionNavigationContentLoader, RegionNavigationContentLoader>();
        //containerRegistry.RegisterSingleton(typeof(IRegionBehaviorFactory),
        //    obj => new RegionBehaviorFactory(_containerExtension));
        //containerRegistry.RegisterSingleton<ItemsControlRegionAdapter, ItemsControlRegionAdapter>();
        //containerRegistry.RegisterSingleton<ContentControlRegionAdapter, ContentControlRegionAdapter>();

        containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();

        containerRegistry.RegisterSingleton<IModuleManager, ModuleManager>();

        containerRegistry.RegisterSingleton<IModuleInitializer, ModuleInitializer>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
    }
}