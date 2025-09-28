using System;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using AvaloniaApplication.Views;
using Prism;
using Prism.Ioc;
using Ava.Xioa.Common.Extensions;
using Ava.Xioa.Common.Models;
using Ava.Xioa.InfrastructureModule;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Configuration;
using Prism.Container.DryIoc;
using Prism.Events;
using Prism.Modularity;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace AvaloniaApplication;

public partial class App : PrismApplicationBase
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    protected override IContainerExtension CreateContainerExtension()
    {
        return new DryIocContainerExtension();
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        moduleCatalog.AddModule(typeof(InfrastructureModule));
    }

    protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
    {
        base.ConfigureRegionAdapterMappings(regionAdapterMappings);
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        IConfiguration configuration = new ConfigurationManager()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        // 注册区域相关服务
        containerRegistry.RegisterSingleton<IRegionManager, RegionManager>();
        containerRegistry.RegisterSingleton<IRegionNavigationContentLoader, RegionNavigationContentLoader>();
        containerRegistry.RegisterSingleton<IRegionBehaviorFactory, RegionBehaviorFactory>();

        containerRegistry.RegisterSingleton<IModuleInitializer, ModuleInitializer>();
        containerRegistry.RegisterSingleton<IModuleManager, ModuleManager>();
        containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();

        containerRegistry.RegisterInstance<IConfiguration>(configuration);

        containerRegistry.RegisterInstance<SystemDbConfig>(configuration.GetSection("SystemDb").Get<SystemDbConfig>()
                                                           ?? throw new Exception("SystemDb配置项缺失"));

        containerRegistry
            .AddPrismAutoDbContext()
            .AddPrismAutoRepository()
            .AddPrismServices()
            .AddPrismVms()
            .AddPrismViews();

        containerRegistry.RegisterSingleton<ISukiToastManager, SukiToastManager>();
        containerRegistry.RegisterSingleton<ISukiDialogManager, SukiDialogManager>();
    }

    protected override AvaloniaObject CreateShell()
    {
        var mainView = Container.Resolve<MainView>();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(mainView);
            return desktop.MainWindow;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = mainView;
            return singleView.MainView;
        }

        return null;
    }

    protected override void OnInitialized()
    {
        DisableAvaloniaDataAnnotationValidation();
        base.OnInitialized();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 初始化托盘图标
            InitializeTrayIcon();
        }
    }
}