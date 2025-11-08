using System;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Ava.Xioa.Common.Events;
using Avalonia.Markup.Xaml;
using AvaloniaApplication.Views;
using Prism;
using Prism.Ioc;
using Ava.Xioa.Common.Extensions;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.InfrastructureModule;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaApplication.ViewModels;
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
        SubscribeMessage();
        ConfigureLangManager();
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


        containerRegistry.RegisterInstance<IApplicationLifetime>(ApplicationLifetime!);


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

        GlobalEventAggregator.EventAggregator = Container.Resolve<IEventAggregator>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowViewModel = Container.Resolve<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow(mainView, mainWindowViewModel);
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
        // var regionManager = Container.Resolve<IRegionManager>();
        // regionManager.RequestNavigate("SplashView", AppRegions.MainRegion);
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
        if (Detection)
        {
            throw new Exception("检测到程序正在运行，请关闭所有程序后重新运行");
        }


        base.OnFrameworkInitializationCompleted();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 初始化托盘图标
            InitializeTrayIcon();
        }

        if (_eventAggregator is not null)
        {
            _eventAggregator.GetEvent<ExitApplicationEvent>().Subscribe(
                (x) => ExitApplication(x.Value.ExitCode)
                , ThreadOption.UIThread, true,
                filter => filter.TokenKey == "ExitApplication");
        }
    }
}