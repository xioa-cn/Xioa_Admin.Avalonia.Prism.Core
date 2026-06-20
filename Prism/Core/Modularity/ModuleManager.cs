using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

namespace Prism.Modularity;

public sealed class ModuleManager : IModuleManager
{
    private readonly IModuleCatalog _catalog;
    private readonly IContainerProvider _containerProvider;
    private readonly IContainerRegistry _containerRegistry;
    private readonly HashSet<string> _loadedModules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IModule> _preRegisteredModuleInstances = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IDisposable> _preRegisteredModuleProviders = new(StringComparer.Ordinal);

    public ModuleManager(IModuleCatalog catalog, IContainerProvider containerProvider, IContainerRegistry containerRegistry)
    {
        _catalog = catalog;
        _containerProvider = containerProvider;
        _containerRegistry = containerRegistry;
    }

    public event EventHandler<LoadModuleCompletedEventArgs>? LoadModuleCompleted;

    public event EventHandler<ModuleDownloadProgressChangedEventArgs>? ModuleDownloadProgressChanged;

    public void Run()
    {
        RegisterModules();
        InitializeModules();
    }

    public void RegisterModules()
    {
        foreach (var moduleInfo in SortByDependencies(_catalog.Modules.Where(module => module.InitializationMode == InitializationMode.WhenAvailable)))
        {
            RegisterModule(moduleInfo.ModuleName, useDynamicPartition: false);
        }
    }

    public void LoadModule(string moduleName)
    {
        var moduleInfo = _catalog.Modules.FirstOrDefault(module => string.Equals(module.ModuleName, moduleName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Module '{moduleName}' is not in the module catalog.");

        if (_loadedModules.Contains(moduleInfo.ModuleName))
        {
            RaiseLoadModuleCompleted(moduleInfo, null);
            return;
        }

        Exception? error = null;
        try
        {
            RegisterModule(moduleInfo.ModuleName, useDynamicPartition: true);
            InitializeModule(moduleInfo);
        }
        catch (Exception ex)
        {
            error = ex;
            if (moduleInfo.State != ModuleState.Initialized)
            {
                moduleInfo.State = ModuleState.NotStarted;
            }
        }

        RaiseLoadModuleCompleted(moduleInfo, error);
        if (error is not null)
        {
            throw error;
        }
    }

    private void RegisterModule(string moduleName, bool useDynamicPartition)
    {
        var moduleInfo = _catalog.Modules.FirstOrDefault(module => string.Equals(module.ModuleName, moduleName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Module '{moduleName}' is not in the module catalog.");

        if (moduleInfo.State >= ModuleState.ReadyForInitialization)
        {
            return;
        }

        foreach (var dependencyName in moduleInfo.DependsOn)
        {
            RegisterModule(dependencyName, useDynamicPartition);
        }

        RegisterModuleTypes(moduleInfo, useDynamicPartition);
    }

    private void RegisterModuleTypes(ModuleInfo moduleInfo, bool useDynamicPartition)
    {
        moduleInfo.State = ModuleState.LoadingTypes;
        var moduleType = ResolveModuleType(moduleInfo);
        if (!_containerProvider.IsRegistered(moduleType))
        {
            _containerRegistry.Register(moduleType);
        }

        ModuleDownloadProgressChanged?.Invoke(this, new ModuleDownloadProgressChangedEventArgs(moduleInfo, 1, 1));
        var module = CreateModuleForRegistration(moduleInfo, moduleType, useDynamicPartition);

        if (useDynamicPartition && _containerRegistry is Prism.Ioc.IDynamicContainerRegistry dynamicContainerRegistry)
        {
            using var moduleDynamic = dynamicContainerRegistry.BeginModuleDynamic(moduleInfo.ModuleName);
            module.RegisterTypes(_containerRegistry);
        }
        else
        {
            module.RegisterTypes(_containerRegistry);
        }
        moduleInfo.State = ModuleState.ReadyForInitialization;
    }

    private IModule CreateModuleForRegistration(ModuleInfo moduleInfo, Type moduleType, bool useDynamicPartition)
    {
        if (useDynamicPartition ||
            _containerRegistry is not ContainerRegistry containerRegistry ||
            containerRegistry.IsRootProviderBuilt)
        {
            return (IModule)_containerProvider.Resolve(moduleType);
        }

        var provider = containerRegistry.Services.BuildServiceProvider();
        try
        {
            var module = (IModule)provider.GetRequiredService(moduleType);
            _preRegisteredModuleInstances[moduleInfo.ModuleName] = module;
            _preRegisteredModuleProviders[moduleInfo.ModuleName] = provider;
            return module;
        }
        catch
        {
            provider.Dispose();
            throw;
        }
    }

    public void InitializeModules()
    {
        foreach (var moduleInfo in SortByDependencies(_catalog.Modules.Where(module => module.InitializationMode == InitializationMode.WhenAvailable)))
        {
            InitializeModule(moduleInfo);
        }
    }

    private void InitializeModule(ModuleInfo moduleInfo)
    {
        if (_loadedModules.Contains(moduleInfo.ModuleName))
        {
            return;
        }

        moduleInfo.State = ModuleState.Initializing;
        var moduleType = ResolveModuleType(moduleInfo);
        var module = _preRegisteredModuleInstances.Remove(moduleInfo.ModuleName, out var preRegisteredModule)
            ? preRegisteredModule
            : (IModule)_containerProvider.Resolve(moduleType);
        try
        {
            module.OnInitialized(_containerProvider);
            moduleInfo.State = ModuleState.Initialized;
            _loadedModules.Add(moduleInfo.ModuleName);
        }
        finally
        {
            if (_preRegisteredModuleProviders.Remove(moduleInfo.ModuleName, out var provider))
            {
                provider.Dispose();
            }
        }
    }

    private Type ResolveModuleType(ModuleInfo moduleInfo)
    {
        return moduleInfo.ModuleType;
        
        if (!string.IsNullOrWhiteSpace(moduleInfo.Ref))
        {
            var path = moduleInfo.Ref!;
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.IsFile)
            {
                path = uri.LocalPath;
            }

            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path, AppContext.BaseDirectory);
            }

            if (File.Exists(path))
            {
                Assembly.LoadFrom(path);
            }
            else if (Directory.Exists(path))
            {
                foreach (var assemblyPath in Directory.EnumerateFiles(path, "*.dll"))
                {
                    Assembly.LoadFrom(assemblyPath);
                }
            }
        }

        // return Type.GetType(moduleInfo.ModuleType, throwOnError: true)!;
    }

    private IEnumerable<ModuleInfo> SortByDependencies(IEnumerable<ModuleInfo> modules)
    {
        var byName = _catalog.Modules.ToDictionary(module => module.ModuleName, StringComparer.Ordinal);
        var requested = modules.Select(module => module.ModuleName).ToHashSet(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var sorted = new List<ModuleInfo>();

        foreach (var moduleName in requested)
        {
            Visit(moduleName);
        }

        return sorted;

        void Visit(string moduleName)
        {
            if (visited.Contains(moduleName))
            {
                return;
            }

            if (!byName.TryGetValue(moduleName, out var moduleInfo))
            {
                throw new InvalidOperationException($"Module '{moduleName}' is required but was not found in the module catalog.");
            }

            if (!visiting.Add(moduleName))
            {
                throw new InvalidOperationException($"Cyclic module dependency detected at '{moduleName}'.");
            }

            foreach (var dependencyName in moduleInfo.DependsOn)
            {
                Visit(dependencyName);
            }

            visiting.Remove(moduleName);
            visited.Add(moduleName);
            if (requested.Contains(moduleName))
            {
                sorted.Add(moduleInfo);
            }
        }
    }

    private void RaiseLoadModuleCompleted(ModuleInfo moduleInfo, Exception? error)
    {
        var args = new LoadModuleCompletedEventArgs(moduleInfo, error);
        LoadModuleCompleted?.Invoke(this, args);
    }
}
