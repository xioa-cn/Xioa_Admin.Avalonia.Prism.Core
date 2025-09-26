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

        containerRegistry.AddPrismServices().AddPrismVms().AddPrismViews();
    }

    protected override Window CreateShell()
    {
        var indexWindow = Container.Resolve<MainWindow>();

        return indexWindow;
    }

    protected override void OnInitialized()
    {
        // 禁用 Avalonia 的数据验证插件，避免与 CommunityToolkit 重复
        DisableAvaloniaDataAnnotationValidation();
        base.OnInitialized();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}