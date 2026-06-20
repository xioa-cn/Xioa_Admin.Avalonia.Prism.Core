using System;

namespace Prism.Modularity;

public static class IModuleCatalogExtensions
{
    public static IModuleCatalog AddModule<T>(this IModuleCatalog catalog, InitializationMode mode = InitializationMode.WhenAvailable, params string[] dependsOn)
        where T : IModule
    {
        return catalog.AddModule<T>(typeof(T).Name, mode, dependsOn);
    }

    public static IModuleCatalog AddModule<T>(this IModuleCatalog catalog, string name, InitializationMode mode = InitializationMode.WhenAvailable, params string[] dependsOn)
        where T : IModule
    {
        return catalog.AddModule(name, typeof(T), mode, dependsOn);
    }

    public static IModuleCatalog AddModule(this IModuleCatalog catalog, Type moduleType, params string[] dependsOn)
    {
        return catalog.AddModule(moduleType, InitializationMode.WhenAvailable, dependsOn);
    }

    public static IModuleCatalog AddModule(this IModuleCatalog catalog, Type moduleType, InitializationMode initializationMode, params string[] dependsOn)
    {
        if (moduleType is null)
        {
            throw new ArgumentNullException(nameof(moduleType));
        }

        return catalog.AddModule(moduleType.Name, moduleType, initializationMode, dependsOn);
    }

    public static IModuleCatalog AddModule(this IModuleCatalog catalog, string moduleName, Type moduleType, params string[] dependsOn)
    {
        return catalog.AddModule(moduleName, moduleType, InitializationMode.WhenAvailable, dependsOn);
    }

    public static IModuleCatalog AddModule(this IModuleCatalog catalog, string moduleName, Type moduleType, InitializationMode initializationMode, params string[] dependsOn)
    {
        return catalog.AddModule(moduleName, moduleType, null, initializationMode, dependsOn);
    }

    public static IModuleCatalog AddModule(this IModuleCatalog catalog, string moduleName, Type moduleType, string? refValue, InitializationMode initializationMode, params string[] dependsOn)
    {
        if (moduleName is null)
        {
            throw new ArgumentNullException(nameof(moduleName));
        }

        if (moduleType is null)
        {
            throw new ArgumentNullException(nameof(moduleType));
        }

        var moduleInfo = new ModuleInfo(moduleName, moduleType, dependsOn)
        {
            InitializationMode = initializationMode,
            Ref = refValue
        };

        return catalog.AddModule(moduleInfo);
    }

    public static IModuleCatalog AddModule<T>(this IModuleCatalog catalog, string name)
        where T : IModule
    {
        return catalog.AddModule<T>(name, InitializationMode.WhenAvailable);
    }

    public static IModuleCatalog AddModule<T>(this IModuleCatalog catalog, string name, InitializationMode mode)
        where T : IModule
    {
        return catalog.AddModule(new ModuleInfo(typeof(T), name, mode));
    }

    public static IModuleCatalog AddGroup(this IModuleCatalog catalog, InitializationMode initializationMode, string refValue, params ModuleInfo[] moduleInfos)
    {
        if (catalog is not IModuleGroupsCatalog moduleGroupsCatalog)
        {
            throw new NotSupportedException("Catalog must support module groups.");
        }

        if (moduleInfos is null)
        {
            throw new ArgumentNullException(nameof(moduleInfos));
        }

        var group = new ModuleInfoGroup
        {
            InitializationMode = initializationMode,
            Ref = refValue
        };

        foreach (var moduleInfo in moduleInfos)
        {
            moduleInfo.InitializationMode = initializationMode;
            moduleInfo.Ref ??= refValue;
            group.Add(moduleInfo);
            catalog.AddModule(moduleInfo);
        }

        moduleGroupsCatalog.Items.Add(group);
        return catalog;
    }
}