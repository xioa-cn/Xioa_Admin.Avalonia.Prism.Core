using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Prism.Dialogs;

namespace Prism;

internal static class PrismInitializationExtensions
{
    internal static void ConfigureViewModelLocator()
    {
        ViewModelLocationProvider.SetDefaultViewModelFactory(type => ContainerLocator.Container.Resolve(type));
    }

    internal static void RegisterRequiredTypes(this IContainerRegistry containerRegistry, IModuleCatalog moduleCatalog)
    {
        containerRegistry.RegisterInstance<IContainerProvider>(ContainerLocator.Container);
        containerRegistry.RegisterInstance<IContainerRegistry>(ContainerLocator.Container);
        containerRegistry.RegisterInstance<IContainerExtension>(ContainerLocator.Container);

        containerRegistry.RegisterInstance<IModuleCatalog>(moduleCatalog);
        containerRegistry.RegisterSingleton<IDialogService, DialogService>();
        containerRegistry.RegisterSingleton<IModuleInitializer, ModuleInitializer>();
        containerRegistry.RegisterSingleton<IModuleManager, ModuleManager>();
        containerRegistry.RegisterSingleton<RegionAdapterMappings>();
        containerRegistry.RegisterSingleton<IRegionManager, RegionManager>();
        containerRegistry.RegisterSingleton<IRegionViewFactory, ContainerRegionViewFactory>();
        containerRegistry.RegisterSingleton<IRegionNavigationContentLoader, RegionNavigationContentLoader>();
        containerRegistry.RegisterSingleton<IRegionViewRegistry, RegionViewRegistry>();
        containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();
        containerRegistry.RegisterSingleton<IRegionBehaviorFactory, RegionBehaviorFactory>();
        containerRegistry.Register<IRegionNavigationService, RegionNavigationService>();
        containerRegistry.Register<IDialogWindow, DialogWindow>();
        containerRegistry.RegisterInstance<IRegionNavigationLogger>(NullRegionNavigationLogger.Instance);
        containerRegistry.Register<IRegionNavigationJournalEntry, RegionNavigationJournalEntry>();

        containerRegistry.Register<ContentControlRegionAdapter>();
        containerRegistry.Register<PanelRegionAdapter>();
        containerRegistry.Register<SingleActivePanelRegionAdapter>();
        containerRegistry.Register<ItemsControlRegionAdapter>();
        containerRegistry.Register<RegionManagerRegistrationBehavior>();
        containerRegistry.Register<AutoPopulateRegionBehavior>();
        containerRegistry.Register<RegionActiveAwareBehavior>();
        containerRegistry.Register<RegionMemberLifetimeBehavior>();

    }

    internal static void RegisterModuleManager(IContainerProvider containerProvider)
    {
        containerProvider.Resolve<IModuleManager>().RegisterModules();
    }

    internal static void RunModuleManager(IContainerProvider containerProvider)
    {
        containerProvider.Resolve<IModuleManager>().InitializeModules();
    }
}
