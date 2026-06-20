using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Prism.Common;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace Prism;

public abstract class PrismApplicationBase : Application
{
    private IContainerExtension? _containerExtension;
    private IModuleCatalog? _moduleCatalog;

    public AvaloniaObject? MainWindow { get; private set; }

    public IContainerProvider Container => _containerExtension ??
                                           throw new InvalidOperationException(
                                               "The Prism container has not been initialized.");

    protected virtual void ConfigureViewModelLocator()
    {
        PrismInitializationExtensions.ConfigureViewModelLocator();
    }

    public override void Initialize()
    {
        base.Initialize();
        ConfigureViewModelLocator();
        ContainerLocator.SetContainerExtension(CreateContainerExtension());
        _containerExtension = ContainerLocator.Container;
        _moduleCatalog = CreateModuleCatalog();
        RegisterRequiredTypes(_containerExtension);
        RegisterTypes(_containerExtension);
        ConfigureModuleCatalog(_moduleCatalog);
        
        ConfigureRegionAdapterMappings(_containerExtension.Resolve<RegionAdapterMappings>());
        ConfigureDefaultRegionBehaviors(_containerExtension.Resolve<IRegionBehaviorFactory>());
        RegisterFrameworkExceptionTypes();
        InitializeModules();


        var shell = CreateShell();
        if (shell is not null)
        {
            MvvmHelpers.AutowireViewModel(shell);
            RegionManager.SetRegionManager(shell, _containerExtension.Resolve<IRegionManager>());
            RegionManager.UpdateRegions(shell);
            InitializeShell(shell);
        }

        OnInitialized();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = MainWindow as Window;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = MainWindow as Control;
        }

        base.OnFrameworkInitializationCompleted();
    }

    protected virtual IContainerExtension CreateContainerExtension() => new ContainerRegistry();

    protected virtual IModuleCatalog CreateModuleCatalog() => new ModuleCatalog();

    protected virtual void RegisterRequiredTypes(IContainerRegistry containerRegistry)
    {
        if (_moduleCatalog is null)
        {
            throw new InvalidOperationException("The module catalog has not been initialized.");
        }

        containerRegistry.RegisterRequiredTypes(_moduleCatalog);
    }

    protected abstract void RegisterTypes(IContainerRegistry containerRegistry);

    protected virtual void ConfigureDefaultRegionBehaviors(IRegionBehaviorFactory regionBehaviors)
    {
        regionBehaviors?.RegisterDefaultRegionBehaviors();
    }

    protected virtual void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
    {
        regionAdapterMappings?.RegisterDefaultRegionAdapterMappings();
    }

    protected virtual void RegisterFrameworkExceptionTypes()
    {
    }

    protected abstract AvaloniaObject CreateShell();

    protected virtual void InitializeShell(AvaloniaObject shell)
    {
        MainWindow = shell;
    }

    protected virtual void OnInitialized()
    {
        if (MainWindow is Window window)
        {
            window.Show();
        }
    }

    protected virtual void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
    }

    protected virtual void InitializeModules()
    {
        PrismInitializationExtensions.RegisterModuleManager(Container);
        PrismInitializationExtensions.RunModuleManager(Container);
    }
}

public abstract class PrismApplication : PrismApplicationBase
{
}