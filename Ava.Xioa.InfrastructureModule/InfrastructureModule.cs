using Ava.Xioa.Common.Modularity;
using Ava.Xioa.Connectlayer.Global;
using Ava.Xioa.Entities.SystemDbset;
using Ava.Xioa.InfrastructureModule.Animation;
using Prism.Ioc;
using Prism.Navigation.Regions;

namespace Ava.Xioa.InfrastructureModule;

public class InfrastructureModule : PrismAutoModule<InfrastructureModule>
{
    // private readonly SystemDbContext _systemDbContext;
    //
    // public InfrastructureModule(SystemDbContext systemDbContext)
    // {
    //     _systemDbContext = systemDbContext;
    // }

    public override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<IRegionNavigationAnimation, HomeRegionAnimation>(AppRegions.HomeRegion);
        base.RegisterTypes(containerRegistry);
    }

    public override async void OnInitialized(IContainerProvider containerProvider)
    {
        //await _systemDbContext.DbFileExistOrCreateAsync();
        base.OnInitialized(containerProvider);
    }
}