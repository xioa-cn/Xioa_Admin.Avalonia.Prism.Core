using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using AvaloniaApplication.Views;
using Microsoft.Extensions.DependencyInjection;
using Prism;
using Prism.Ioc;
using Prism.Microsoft.DependencyInjection;
using Ava.Xioa.Common.Extensions;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Prism.Events;
using Prism.Modularity;
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
        var services = new ServiceCollection();

        return new PrismContainerExtension(services) as IContainerExtension;
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IModuleCatalog, ModuleCatalog>();
        containerRegistry.RegisterSingleton<IModuleInitializer, ModuleInitializer>();
        containerRegistry.RegisterSingleton<IModuleManager, ModuleManager>();
        containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();

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