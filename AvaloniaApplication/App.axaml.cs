using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using AvaloniaApplication.Views;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Prism;
using Prism.Ioc;
using Prism.Microsoft.DependencyInjection;
using Ava.Xioa.Common.Extensions;
using Avalonia.Controls.ApplicationLifetimes;
using Prism.Modularity;

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

        containerRegistry
            .AddPrismAutoDbContext()
            .AddPrismAutoRepository()
            .AddPrismServices()
            .AddPrismVms()
            .AddPrismViews();
    }

    protected override Window CreateShell()
    {
        var indexWindow = Container.Resolve<MainWindow>();

        return indexWindow;
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