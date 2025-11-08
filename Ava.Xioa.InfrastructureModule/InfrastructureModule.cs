using Ava.Xioa.Common.Modularity;
using Ava.Xioa.Entities.SystemDbset;
using Prism.Ioc;

namespace Ava.Xioa.InfrastructureModule;

public class InfrastructureModule : PrismAutoModule<InfrastructureModule>
{
    private readonly SystemDbContext _systemDbContext;

    public InfrastructureModule(SystemDbContext systemDbContext)
    {
        _systemDbContext = systemDbContext;
    }

    public override async void OnInitialized(IContainerProvider containerProvider)
    {
        //await _systemDbContext.DbFileExistOrCreateAsync();
        base.OnInitialized(containerProvider);
    }
}