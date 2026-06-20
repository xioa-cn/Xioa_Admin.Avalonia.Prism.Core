using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Ioc;

namespace Prism.Modularity;

public class ModuleCatalog : IModuleCatalog, IModuleGroupsCatalog
{
    private readonly List<ModuleInfo> _modules = new();

    public IEnumerable<ModuleInfo> Modules => _modules;

    public IList<IModuleCatalogItem> Items { get; } = new List<IModuleCatalogItem>();

    public IModuleCatalog AddModule(ModuleInfo moduleInfo)
    {
        if (_modules.Any(existing => string.Equals(existing.ModuleName, moduleInfo.ModuleName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Module '{moduleInfo.ModuleName}' has already been added.");
        }
        
        ContainerLocator.Container.Register(moduleInfo.ModuleType, moduleInfo.ModuleType);

        _modules.Add(moduleInfo);
        return this;
    }

    public IModuleCatalog AddModule(IModuleInfo moduleInfo)
    {
        if (moduleInfo is ModuleInfo concrete)
        {
            return AddModule(concrete);
        }

        var copy = new ModuleInfo
        {
            ModuleName = moduleInfo.ModuleName,
            ModuleType = moduleInfo.ModuleType,
            InitializationMode = moduleInfo.InitializationMode,
            State = moduleInfo.State,
            Ref = moduleInfo.Ref
        };

        foreach (var dependency in moduleInfo.DependsOn)
        {
            copy.DependsOn.Add(dependency);
        }

        return AddModule(copy);
    }
}