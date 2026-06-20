using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Prism.Mvvm;

namespace Prism.Ioc;

public sealed class ContainerRegistry : IContainerExtension, IDynamicContainerRegistry, IDynamicContainerPartition
{
    private readonly IServiceCollection _services;
    private readonly DynamicServiceContainer _dynamicServices = new();
    private readonly Dictionary<string, DynamicServiceContainer> _moduleDynamicServices = new(StringComparer.Ordinal);
    private readonly object _syncRoot = new();
    private readonly AsyncLocal<string?> _currentModuleName = new();
    private IServiceProvider? _serviceProvider;
    private bool _rootProviderBuilt;

    public ContainerRegistry()
        : this(new ServiceCollection())
    {
    }

    public ContainerRegistry(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services => _services;

    public bool IsRootProviderBuilt
    {
        get
        {
            lock (_syncRoot)
            {
                return _rootProviderBuilt;
            }
        }
    }

    public IServiceProvider ServiceProvider
    {
        get
        {
            lock (_syncRoot)
            {
                _rootProviderBuilt = true;
                return _serviceProvider ??= _services.BuildServiceProvider();
            }
        }
    }

    public IContainerRegistry Register(Type from, Type to)
    {
        AddRegistration(from, null, ServiceLifetime.Transient, to, null, null);
        return this;
    }

    public IContainerRegistry Register(Type type) => Register(type, type);
    public IContainerRegistry RegisterScoped(Type from, Type to)
    {
        AddRegistration(from, null, ServiceLifetime.Scoped, to, null, null);
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type from, Type to)
    {
        AddRegistration(from, null, ServiceLifetime.Singleton, to, null, null);
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type type) => RegisterSingleton(type, type);

    public IContainerRegistry RegisterInstance(Type type, object instance)
    {
        AddRegistration(type, null, ServiceLifetime.Singleton, null, instance, null);
        return this;
    }

    public IContainerRegistry Register(Type from, Type to, string name)
    {
        AddRegistration(from, name, ServiceLifetime.Transient, to, null, null);
        return this;
    }

    public IContainerRegistry Register(Type type, string name) => Register(type, type, name);

    public IContainerRegistry RegisterSingleton(Type from, Type to, string name)
    {
        AddRegistration(from, name, ServiceLifetime.Singleton, to, null, null);
        return this;
    }

    public IContainerRegistry RegisterInstance(Type type, object instance, string name)
    {
        AddRegistration(type, name, ServiceLifetime.Singleton, null, instance, null);
        return this;
    }

    public IContainerRegistry RegisterFactory(Type type, Func<IContainerProvider, object> factory)
    {
        AddRegistration(type, null, ServiceLifetime.Transient, null, null, factory);
        return this;
    }

    public IContainerRegistry RegisterFactory(Type type, Func<IContainerProvider, object> factory, string name)
    {
        AddRegistration(type, name, ServiceLifetime.Transient, null, null, factory);
        return this;
    }

    public IContainerRegistry RegisterSingletonFactory(Type type, Func<IContainerProvider, object> factory)
    {
        AddRegistration(type, null, ServiceLifetime.Singleton, null, null, factory);
        return this;
    }

    public IContainerRegistry RegisterSingletonFactory(Type type, Func<IContainerProvider, object> factory, string name)
    {
        AddRegistration(type, name, ServiceLifetime.Singleton, null, null, factory);
        return this;
    }

    public object Resolve(Type type) => Resolve(type, null);

    public object Resolve(Type type, string? name)
    {
        try
        {
            if (TryResolveModuleDynamic(_currentModuleName.Value, type, name, this, out var moduleService))
            {
                return moduleService;
            }

            if (TryResolveDynamic(type, name, this, out var dynamicService))
            {
                return dynamicService;
            }

            if (TryResolveFromServices(type, name, ServiceProvider, out var service))
            {
                return service;
            }

        }
        catch (Exception ex) when (ex is not ContainerResolutionException)
        {
            throw CreateResolutionException(type, name, ex);
        }

        throw CreateResolutionException(type, name, null);
    }

    public T Resolve<T>() => (T)Resolve(typeof(T));

    public T Resolve<T>(string name) => (T)Resolve(typeof(T), name);

    public bool IsRegistered(Type type) => IsRegisteredCore(type, null);

    public bool IsRegistered(Type type, string name) => IsRegisteredCore(type, name);

    public IContainerProvider CreateScope()
    {
        return new ScopedContainerProvider(this, ServiceProvider.CreateScope(), _currentModuleName.Value);
    }

    public IContainerProvider CreateScope(string? moduleName)
    {
        return new ScopedContainerProvider(this, ServiceProvider.CreateScope(), moduleName);
    }

    public IDisposable BeginModuleDynamic(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new ArgumentException("Module name cannot be empty.", nameof(moduleName));
        }

        var previousModuleName = _currentModuleName.Value;
        _currentModuleName.Value = moduleName;
        return new ModuleDynamicScope(this, previousModuleName);
    }

    public IDisposable ResetModuleDynamicContext()
    {
        var previousModuleName = _currentModuleName.Value;
        _currentModuleName.Value = null;
        return new ModuleDynamicScope(this, previousModuleName);
    }

    public void ClearModuleDynamic(string moduleName)
    {
        lock (_syncRoot)
        {
            if (_moduleDynamicServices.Remove(moduleName, out var services))
            {
                services.Dispose();
            }
        }
    }

    public bool Unregister(Type type, string? name = null)
    {
        lock (_syncRoot)
        {
            return _dynamicServices.Remove(type, name) ||
                   RemoveModuleDynamicRegistration(type, name);
        }
    }

    public IContainerRegistry CreateScopeRegistry(string? moduleName = null)
    {
        return new ScopedContainerProvider(this, ServiceProvider.CreateScope(), moduleName ?? _currentModuleName.Value);
    }

    private bool IsRegisteredCore(Type type, string? name)
    {
        return IsRegisteredInDynamic(type, name) || IsRegisteredInModuleDynamic(_currentModuleName.Value, type, name) || IsRegisteredInServices(type, name);
    }

    private void AddRegistration(
        Type serviceType,
        string? name,
        ServiceLifetime lifetime,
        Type? implementationType,
        object? instance,
        Func<IContainerProvider, object>? factory)
    {
        lock (_syncRoot)
        {
            if (!_rootProviderBuilt)
            {
                AddServiceDescriptor(serviceType, name, lifetime, implementationType, instance, factory);
                return;
            }

            if (_currentModuleName.Value is { Length: > 0 } moduleName)
            {
                if (!_moduleDynamicServices.TryGetValue(moduleName, out var dynamicServices))
                {
                    dynamicServices = new DynamicServiceContainer();
                    _moduleDynamicServices[moduleName] = dynamicServices;
                }

                dynamicServices.Add(serviceType, name, lifetime, implementationType, instance, factory);
                return;
            }

            _dynamicServices.Add(serviceType, name, lifetime, implementationType, instance, factory);
        }
    }

    private void AddServiceDescriptor(
        Type serviceType,
        string? name,
        ServiceLifetime lifetime,
        Type? implementationType,
        object? instance,
        Func<IContainerProvider, object>? factory)
    {
        AddServiceDescriptorTo(_services, this, serviceType, name, lifetime, implementationType, instance, factory);
    }

    private static void AddServiceDescriptorTo(
        IServiceCollection services,
        IContainerProvider containerProvider,
        Type serviceType,
        string? name,
        ServiceLifetime lifetime,
        Type? implementationType,
        object? instance,
        Func<IContainerProvider, object>? factory)
    {
        if (name is { Length: > 0 })
        {
            if (instance is not null)
            {
                services.AddKeyedSingleton(serviceType, name, instance);
            }
            else if (factory is not null)
            {
                AddKeyedFactory(services, containerProvider, serviceType, name, lifetime, factory);
            }
            else
            {
                AddKeyedType(services, serviceType, name, lifetime, implementationType ?? serviceType);
            }

            return;
        }

        if (instance is not null)
        {
            services.AddSingleton(serviceType, instance);
        }
        else if (factory is not null)
        {
            services.Add(new ServiceDescriptor(serviceType, _ => factory(containerProvider), lifetime));
        }
        else
        {
            services.Add(new ServiceDescriptor(serviceType, implementationType ?? serviceType, lifetime));
        }
    }

    private static void AddKeyedType(IServiceCollection services, Type serviceType, string name, ServiceLifetime lifetime, Type implementationType)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.AddKeyedSingleton(serviceType, name, implementationType);
                break;
            case ServiceLifetime.Scoped:
                services.AddKeyedScoped(serviceType, name, implementationType);
                break;
            default:
                services.AddKeyedTransient(serviceType, name, implementationType);
                break;
        }
    }

    private static void AddKeyedFactory(IServiceCollection services, IContainerProvider containerProvider, Type serviceType, string name, ServiceLifetime lifetime, Func<IContainerProvider, object> factory)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.AddKeyedSingleton(serviceType, name, (_, _) => factory(containerProvider));
                break;
            case ServiceLifetime.Scoped:
                services.AddKeyedScoped(serviceType, name, (_, _) => factory(containerProvider));
                break;
            default:
                services.AddKeyedTransient(serviceType, name, (_, _) => factory(containerProvider));
                break;
        }
    }

    private bool IsRegisteredInServices(Type type, string? name)
    {
        lock (_syncRoot)
        {
            return TryFindServiceDescriptor(type, name, out _);
        }
    }

    private bool IsRegisteredInDynamic(Type type, string? name)
    {
        lock (_syncRoot)
        {
            return _dynamicServices.IsRegistered(type, name);
        }
    }

    private bool IsRegisteredInModuleDynamic(string? moduleName, Type type, string? name)
    {
        lock (_syncRoot)
        {
            if (moduleName is { Length: > 0 })
            {
                return _moduleDynamicServices.TryGetValue(moduleName, out var services) &&
                       services.IsRegistered(type, name);
            }

            return _moduleDynamicServices.Values.Any(services => services.IsRegistered(type, name));
        }
    }

    private static bool MatchesDescriptor(ServiceDescriptor descriptor, Type type, string? name)
    {
        if (descriptor.ServiceType != type)
        {
            return false;
        }

        if (name is { Length: > 0 })
        {
            return descriptor.IsKeyedService && string.Equals(descriptor.ServiceKey?.ToString(), name, StringComparison.Ordinal);
        }

        return !descriptor.IsKeyedService;
    }

    private bool TryResolveFromServices(Type type, string? name, IServiceProvider provider, out object service)
    {
        ServiceDescriptor? descriptor;
        lock (_syncRoot)
        {
            TryFindServiceDescriptor(type, name, out descriptor);
        }

        if (descriptor is null)
        {
            service = null!;
            return false;
        }

        service = name is { Length: > 0 }
            ? provider.GetRequiredKeyedService(descriptor.ServiceType, name)
            : provider.GetRequiredService(descriptor.ServiceType);
        return true;
    }

    private bool TryFindServiceDescriptor(Type type, string? name, out ServiceDescriptor? descriptor)
    {
        descriptor = _services.LastOrDefault(item => MatchesDescriptor(item, type, name));
        if (descriptor is not null)
        {
            return true;
        }

        if (name is not { Length: > 0 })
        {
            return false;
        }

        descriptor = _services.LastOrDefault(item => MatchesAssignableDescriptor(item, type, name));
        return descriptor is not null;
    }

    private static bool MatchesAssignableDescriptor(ServiceDescriptor descriptor, Type type, string name)
    {
        return descriptor.IsKeyedService &&
               string.Equals(descriptor.ServiceKey?.ToString(), name, StringComparison.Ordinal) &&
               type.IsAssignableFrom(descriptor.ServiceType);
    }

    private bool TryResolveDynamic(Type type, string? name, IContainerProvider provider, out object service)
    {
        DynamicServiceContainer dynamicServices;
        lock (_syncRoot)
        {
            dynamicServices = _dynamicServices;
        }

        return TryResolveDynamicContainer(dynamicServices, type, name, provider, out service);
    }

    private bool TryResolveModuleDynamic(string? moduleName, Type type, string? name, IContainerProvider provider, out object service)
    {
        DynamicServiceContainer? dynamicServices;
        lock (_syncRoot)
        {
            if (moduleName is { Length: > 0 })
            {
                dynamicServices = _moduleDynamicServices.TryGetValue(moduleName, out var services)
                    ? services
                    : null;
            }
            else
            {
                dynamicServices = _moduleDynamicServices.Values.LastOrDefault(services => services.IsRegistered(type, name));
            }
        }

        if (dynamicServices is null)
        {
            service = null!;
            return false;
        }

        return TryResolveDynamicContainer(dynamicServices, type, name, provider, out service);
    }

    private bool RemoveModuleDynamicRegistration(Type type, string? name)
    {
        foreach (var services in _moduleDynamicServices.Values)
        {
            if (services.Remove(type, name))
            {
                return true;
            }
        }

        return false;
    }

    private IServiceProvider GetFallbackProvider(IContainerProvider provider)
    {
        return provider is ScopedContainerProvider scoped
            ? scoped.ScopeServiceProvider
            : ServiceProvider;
    }

    private bool TryResolveDynamicContainer(DynamicServiceContainer dynamicServices, Type type, string? name, IContainerProvider provider, out object service)
    {
        var dynamicProvider = provider is ScopedContainerProvider scoped
            ? scoped.GetDynamicServiceProvider(dynamicServices)
            : dynamicServices.ServiceProvider;

        return dynamicServices.TryResolve(type, name, provider, dynamicProvider, GetFallbackProvider(provider), out service);
    }

    private ContainerResolutionException CreateResolutionException(Type type, string? name, Exception? innerException)
    {
        var registered = string.Join(", ", GetRegisteredServices().Take(20));
        var message = $"Failed to resolve service '{type.FullName}'" +
                      (name is { Length: > 0 } ? $" with name '{name}'" : string.Empty) +
                      "." +
                      Environment.NewLine +
                      $"Registered services sample: {(string.IsNullOrWhiteSpace(registered) ? "<none>" : registered)}";
        return new ContainerResolutionException(type, name, message, innerException);
    }

    private IEnumerable<string> GetRegisteredServices()
    {
        lock (_syncRoot)
        {
            foreach (var descriptor in _services)
            {
                yield return descriptor.IsKeyedService
                    ? $"{descriptor.ServiceType.FullName}('{descriptor.ServiceKey}')"
                    : descriptor.ServiceType.FullName ?? descriptor.ServiceType.Name;
            }

            foreach (var registration in _dynamicServices.GetRegistrationDescriptions())
            {
                yield return registration;
            }

            foreach (var (moduleName, services) in _moduleDynamicServices)
            {
                foreach (var registration in services.GetRegistrationDescriptions())
                {
                    yield return $"module:{moduleName}:{registration}";
                }
            }
        }
    }

    private sealed class ScopedContainerProvider : IContainerProvider, IContainerRegistry, IDynamicContainerPartition, IDisposable
    {
        private readonly ContainerRegistry _root;
        private readonly IServiceScope _scope;
        private readonly ScopedContainerProvider? _parent;
        private readonly DynamicServiceContainer _scopeDynamicServices = new();
        private readonly Dictionary<DynamicServiceContainer, DynamicScopeCache> _dynamicScopeCaches = new();
        private readonly object _syncRoot = new();
        private readonly string? _moduleName;
        private readonly ViewModelLocationScope _viewModelLocationScope;
        private int _disposed;

        public ScopedContainerProvider(ContainerRegistry root, IServiceScope scope, string? moduleName, ScopedContainerProvider? parent = null)
        {
            _root = root;
            _scope = scope;
            _parent = parent;
            _moduleName = moduleName;
            _viewModelLocationScope = ViewModelLocationProvider.CreateScopedViewModelFactory(
                (_, type) => Resolve(type),
                moduleName,
                parent is null ? null : ViewModelLocationProvider.GetScopeId(parent._viewModelLocationScope));
        }

        public IServiceProvider ScopeServiceProvider => _scope.ServiceProvider;

        public object Resolve(Type type) => Resolve(type, null);

        public object Resolve(Type type, string? name)
        {
            ThrowIfDisposed();
            using var viewModelLocationContext = ViewModelLocationProvider.UseContext(_viewModelLocationScope.Context);
            try
            {
                if (TryResolveScopeDynamic(type, name, out var scopeService))
                {
                    return scopeService;
                }

                if (TryResolveAncestorScopeDynamic(type, name, out var ancestorScopeService))
                {
                    return ancestorScopeService;
                }

                if (_root.TryResolveModuleDynamic(_moduleName, type, name, this, out var moduleService))
                {
                    return moduleService;
                }

                if (_root.TryResolveDynamic(type, name, this, out var dynamicService))
                {
                    return dynamicService;
                }

                if (_root.TryResolveFromServices(type, name, _scope.ServiceProvider, out var service))
                {
                    return service;
                }

            }
            catch (Exception ex) when (ex is not ContainerResolutionException)
            {
                throw _root.CreateResolutionException(type, name, ex);
            }

            throw _root.CreateResolutionException(type, name, null);
        }

        public T Resolve<T>() => (T)Resolve(typeof(T));

        public T Resolve<T>(string name) => (T)Resolve(typeof(T), name);

        public bool IsRegistered(Type type) => IsRegisteredCore(type, null);

        public bool IsRegistered(Type type, string name) => IsRegisteredCore(type, name);

        private bool IsRegisteredCore(Type type, string? name)
        {
            ThrowIfDisposed();
            lock (_syncRoot)
            {
                if (_scopeDynamicServices.IsRegistered(type, name))
                {
                    return true;
                }
            }

            return IsRegisteredInAncestorScope(type, name) ||
                   _root.IsRegisteredInModuleDynamic(_moduleName, type, name) ||
                   _root.IsRegisteredInDynamic(type, name) ||
                   _root.IsRegisteredInServices(type, name);
        }

        public IContainerProvider CreateScope()
        {
            ThrowIfDisposed();
            return new ScopedContainerProvider(_root, _scope.ServiceProvider.CreateScope(), _moduleName, this);
        }

        public IContainerProvider CreateScope(string? moduleName)
        {
            ThrowIfDisposed();
            return new ScopedContainerProvider(_root, _scope.ServiceProvider.CreateScope(), moduleName, this);
        }

        public IContainerRegistry Register(Type from, Type to)
        {
            AddScopeRegistration(from, null, ServiceLifetime.Transient, to, null, null);
            return this;
        }

        public IContainerRegistry Register(Type type) => Register(type, type);
        public IContainerRegistry RegisterScoped(Type from, Type to)
        {
            AddScopeRegistration(from, null, ServiceLifetime.Scoped, to, null, null);
            return this;
        }

        public IContainerRegistry RegisterSingleton(Type from, Type to)
        {
            AddScopeRegistration(from, null, ServiceLifetime.Singleton, to, null, null);
            return this;
        }

        public IContainerRegistry RegisterSingleton(Type type) => RegisterSingleton(type, type);

        public IContainerRegistry RegisterInstance(Type type, object instance)
        {
            AddScopeRegistration(type, null, ServiceLifetime.Singleton, null, instance, null);
            return this;
        }

        public IContainerRegistry Register(Type from, Type to, string name)
        {
            AddScopeRegistration(from, name, ServiceLifetime.Transient, to, null, null);
            return this;
        }

        public IContainerRegistry Register(Type type, string name) => Register(type, type, name);

        public IContainerRegistry RegisterSingleton(Type from, Type to, string name)
        {
            AddScopeRegistration(from, name, ServiceLifetime.Singleton, to, null, null);
            return this;
        }

        public IContainerRegistry RegisterInstance(Type type, object instance, string name)
        {
            AddScopeRegistration(type, name, ServiceLifetime.Singleton, null, instance, null);
            return this;
        }

        public IContainerRegistry RegisterFactory(Type type, Func<IContainerProvider, object> factory)
        {
            AddScopeRegistration(type, null, ServiceLifetime.Transient, null, null, factory);
            return this;
        }

        public IContainerRegistry RegisterFactory(Type type, Func<IContainerProvider, object> factory, string name)
        {
            AddScopeRegistration(type, name, ServiceLifetime.Transient, null, null, factory);
            return this;
        }

        public IContainerRegistry RegisterSingletonFactory(Type type, Func<IContainerProvider, object> factory)
        {
            AddScopeRegistration(type, null, ServiceLifetime.Singleton, null, null, factory);
            return this;
        }

        public IContainerRegistry RegisterSingletonFactory(Type type, Func<IContainerProvider, object> factory, string name)
        {
            AddScopeRegistration(type, name, ServiceLifetime.Singleton, null, null, factory);
            return this;
        }

        public bool Unregister(Type type, string? name = null)
        {
            lock (_syncRoot)
            {
                return _scopeDynamicServices.Remove(type, name);
            }
        }

        private void AddScopeRegistration(Type serviceType, string? name, ServiceLifetime lifetime, Type? implementationType, object? instance, Func<IContainerProvider, object>? factory)
        {
            ThrowIfDisposed();
            lock (_syncRoot)
            {
                _scopeDynamicServices.Add(serviceType, name, lifetime, implementationType, instance, factory);
            }
        }

        private bool TryResolveScopeDynamic(Type type, string? name, out object service)
        {
            return _scopeDynamicServices.TryResolve(
                type,
                name,
                this,
                GetDynamicServiceProvider(_scopeDynamicServices),
                _scope.ServiceProvider,
                out service);
        }

        private bool TryResolveAncestorScopeDynamic(Type type, string? name, out object service)
        {
            var visited = new HashSet<ScopedContainerProvider>();
            var depth = 0;
            for (var current = _parent;
                 current is not null &&
                 depth < ViewModelLocationProvider.MaxScopeInheritanceDepth &&
                 visited.Add(current);
                 current = current._parent, depth++)
            {
                if (Volatile.Read(ref current._disposed) == 0 &&
                    current.TryResolveScopeDynamic(type, name, out service))
                {
                    return true;
                }
            }

            service = null!;
            return false;
        }

        private bool IsRegisteredInAncestorScope(Type type, string? name)
        {
            var visited = new HashSet<ScopedContainerProvider>();
            var depth = 0;
            for (var current = _parent;
                 current is not null &&
                 depth < ViewModelLocationProvider.MaxScopeInheritanceDepth &&
                 visited.Add(current);
                 current = current._parent, depth++)
            {
                if (Volatile.Read(ref current._disposed) != 0)
                {
                    continue;
                }

                lock (current._syncRoot)
                {
                    if (current._scopeDynamicServices.IsRegistered(type, name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            lock (_syncRoot)
            {
                _scopeDynamicServices.Dispose();
                foreach (var cache in _dynamicScopeCaches.Values)
                {
                    cache.Dispose();
                }

                _dynamicScopeCaches.Clear();
            }

            _viewModelLocationScope.Dispose();
            _scope.Dispose();
        }

        public IServiceProvider GetDynamicServiceProvider(DynamicServiceContainer dynamicServices)
        {
            ThrowIfDisposed();
            lock (_syncRoot)
            {
                if (!_dynamicScopeCaches.TryGetValue(dynamicServices, out var cache))
                {
                    cache = new DynamicScopeCache();
                    _dynamicScopeCaches[dynamicServices] = cache;
                }

                return cache.GetServiceProvider(dynamicServices, this, _scope.ServiceProvider);
            }
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(ScopedContainerProvider));
            }
        }
    }

    private sealed class ModuleDynamicScope : IDisposable
    {
        private readonly ContainerRegistry _container;
        private readonly string? _previousModuleName;
        private bool _disposed;

        public ModuleDynamicScope(ContainerRegistry container, string? previousModuleName)
        {
            _container = container;
            _previousModuleName = previousModuleName;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _container._currentModuleName.Value = _previousModuleName;
        }
    }

    private sealed class DynamicServiceContainer : IDisposable
    {
        private readonly IServiceCollection _services = new ServiceCollection();
        private readonly Dictionary<DynamicServiceCacheKey, object> _singletonInstances = new();
        private readonly object _syncRoot = new();
        private ServiceProvider? _serviceProvider;
        private int _version;

        public int Version
        {
            get
            {
                lock (_syncRoot)
                {
                    return _version;
                }
            }
        }

        public IServiceProvider ServiceProvider
        {
            get
            {
                lock (_syncRoot)
                {
                    return _serviceProvider ??= BuildProvider();
                }
            }
        }

        public void Add(
            Type serviceType,
            string? name,
            ServiceLifetime lifetime,
            Type? implementationType,
            object? instance,
            Func<IContainerProvider, object>? factory)
        {
            lock (_syncRoot)
            {
                AddDynamicServiceDescriptorTo(_services, serviceType, name, lifetime, implementationType, instance, factory);
                RebuildProvider();
            }
        }

        public bool Remove(Type type, string? name)
        {
            lock (_syncRoot)
            {
                for (var index = _services.Count - 1; index >= 0; index--)
                {
                    if (!MatchesDescriptor(_services[index], type, name))
                    {
                        continue;
                    }

                    _services.RemoveAt(index);
                    ClearCachedSingleton(type, name);
                    RebuildProvider();
                    return true;
                }
            }

            return false;
        }

        public bool IsRegistered(Type type, string? name)
        {
            lock (_syncRoot)
            {
                return TryFindServiceDescriptor(type, name, out _);
            }
        }

        public bool TryResolve(Type type, string? name, IContainerProvider containerProvider, IServiceProvider dynamicProvider, IServiceProvider fallbackProvider, out object service)
        {
            ServiceDescriptor? descriptor;
            lock (_syncRoot)
            {
                TryFindServiceDescriptor(type, name, out descriptor);
            }

            if (descriptor is null)
            {
                service = null!;
                return false;
            }

            using (DynamicFactoryContainerProvider.Use(containerProvider, fallbackProvider))
            {
                service = name is { Length: > 0 }
                    ? dynamicProvider.GetRequiredKeyedService(descriptor.ServiceType, name)
                    : dynamicProvider.GetRequiredService(descriptor.ServiceType);
                return true;
            }
        }

        private bool TryFindServiceDescriptor(Type type, string? name, out ServiceDescriptor? descriptor)
        {
            descriptor = _services.LastOrDefault(item => MatchesDescriptor(item, type, name));
            if (descriptor is not null)
            {
                return true;
            }

            if (name is not { Length: > 0 })
            {
                return false;
            }

            descriptor = _services.LastOrDefault(item => MatchesAssignableDescriptor(item, type, name));
            return descriptor is not null;
        }

        public IReadOnlyList<string> GetRegistrationDescriptions()
        {
            lock (_syncRoot)
            {
                return _services
                    .Select(descriptor => descriptor.IsKeyedService
                        ? $"{descriptor.ServiceType.FullName}('{descriptor.ServiceKey}')"
                        : descriptor.ServiceType.FullName ?? descriptor.ServiceType.Name)
                    .ToArray();
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _serviceProvider?.Dispose();
                _serviceProvider = null;
                _services.Clear();
                foreach (var instance in _singletonInstances.Values)
                {
                    DisposeIfNeeded(instance);
                }

                _singletonInstances.Clear();
                _version++;
            }
        }

        private void AddDynamicServiceDescriptorTo(
            IServiceCollection services,
            Type serviceType,
            string? name,
            ServiceLifetime lifetime,
            Type? implementationType,
            object? instance,
            Func<IContainerProvider, object>? factory)
        {
            if (name is { Length: > 0 })
            {
                if (instance is not null)
                {
                    services.AddKeyedSingleton(serviceType, name, instance);
                }
                else if (factory is not null)
                {
                    AddDynamicKeyedFactory(services, serviceType, name, lifetime, factory);
                }
                else
                {
                    AddDynamicKeyedType(services, serviceType, name, lifetime, implementationType ?? serviceType);
                }

                return;
            }

            if (instance is not null)
            {
                services.AddSingleton(serviceType, instance);
            }
            else if (factory is not null)
            {
                services.Add(new ServiceDescriptor(serviceType, _ => CreateDynamicFactoryService(serviceType, null, lifetime, factory), GetDynamicDescriptorLifetime(lifetime)));
            }
            else
            {
                var targetType = implementationType ?? serviceType;
                services.Add(new ServiceDescriptor(serviceType, provider => CreateDynamicTypeService(provider, serviceType, null, lifetime, targetType), GetDynamicDescriptorLifetime(lifetime)));
            }
        }

        private void AddDynamicKeyedType(IServiceCollection services, Type serviceType, string name, ServiceLifetime lifetime, Type implementationType)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddKeyedTransient(serviceType, name, (provider, _) => CreateDynamicTypeService(provider, serviceType, name, lifetime, implementationType));
                    break;
                case ServiceLifetime.Scoped:
                    services.AddKeyedScoped(serviceType, name, (provider, _) => CreateDynamicTypeService(provider, serviceType, name, lifetime, implementationType));
                    break;
                default:
                    services.AddKeyedTransient(serviceType, name, (provider, _) => CreateDynamicTypeService(provider, serviceType, name, lifetime, implementationType));
                    break;
            }
        }

        private void AddDynamicKeyedFactory(IServiceCollection services, Type serviceType, string name, ServiceLifetime lifetime, Func<IContainerProvider, object> factory)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddKeyedTransient(serviceType, name, (_, _) => CreateDynamicFactoryService(serviceType, name, lifetime, factory));
                    break;
                case ServiceLifetime.Scoped:
                    services.AddKeyedScoped(serviceType, name, (_, _) => CreateDynamicFactoryService(serviceType, name, lifetime, factory));
                    break;
                default:
                    services.AddKeyedTransient(serviceType, name, (_, _) => CreateDynamicFactoryService(serviceType, name, lifetime, factory));
                    break;
            }
        }

        private object CreateDynamicTypeService(IServiceProvider provider, Type serviceType, string? name, ServiceLifetime lifetime, Type implementationType)
        {
            if (lifetime != ServiceLifetime.Singleton)
            {
                return DynamicFactoryContainerProvider.CreateInstance(provider, implementationType);
            }

            return GetOrCreateSingleton(serviceType, name, () => DynamicFactoryContainerProvider.CreateInstance(provider, implementationType));
        }

        private object CreateDynamicFactoryService(Type serviceType, string? name, ServiceLifetime lifetime, Func<IContainerProvider, object> factory)
        {
            if (lifetime != ServiceLifetime.Singleton)
            {
                return factory(DynamicFactoryContainerProvider.Active);
            }

            return GetOrCreateSingleton(serviceType, name, () => factory(DynamicFactoryContainerProvider.Active));
        }

        private object GetOrCreateSingleton(Type serviceType, string? name, Func<object> factory)
        {
            var key = new DynamicServiceCacheKey(serviceType, name);
            lock (_syncRoot)
            {
                if (_singletonInstances.TryGetValue(key, out var existing))
                {
                    return existing;
                }
            }

            var created = factory();
            lock (_syncRoot)
            {
                if (_singletonInstances.TryGetValue(key, out var existing))
                {
                    DisposeIfNeeded(created);
                    return existing;
                }

                _singletonInstances[key] = created;
                return created;
            }
        }

        private void ClearCachedSingleton(Type serviceType, string? name)
        {
            var key = new DynamicServiceCacheKey(serviceType, name);
            if (!_singletonInstances.Remove(key, out var removed))
            {
                return;
            }

            DisposeIfNeeded(removed);
        }

        private static ServiceLifetime GetDynamicDescriptorLifetime(ServiceLifetime lifetime)
        {
            return lifetime == ServiceLifetime.Singleton ? ServiceLifetime.Transient : lifetime;
        }

        private static void DisposeIfNeeded(object instance)
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        internal IServiceScope CreateScope(IServiceProvider fallbackProvider)
        {
            return new DynamicServiceScope(ServiceProvider.CreateScope(), fallbackProvider);
        }

        private ServiceProvider BuildProvider()
        {
            return _services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = false,
                ValidateOnBuild = false
            });
        }

        private void RebuildProvider()
        {
            _serviceProvider?.Dispose();
            _serviceProvider = BuildProvider();
            _version++;
        }

        private sealed class DynamicServiceScope : IServiceScope
        {
            private readonly IServiceScope _scope;

            public DynamicServiceScope(IServiceScope scope, IServiceProvider fallbackProvider)
            {
                _scope = scope;
                ServiceProvider = new CompositeFallbackServiceProvider(_scope.ServiceProvider, fallbackProvider);
            }

            public IServiceProvider ServiceProvider { get; }

            public void Dispose()
            {
                _scope.Dispose();
            }
        }
    }

    private readonly record struct DynamicServiceCacheKey(Type ServiceType, string? Name);

    private sealed class DynamicScopeCache : IDisposable
    {
        private IServiceScope? _scope;
        private int _version = -1;

        public IServiceProvider GetServiceProvider(DynamicServiceContainer services, IContainerProvider containerProvider, IServiceProvider fallbackProvider)
        {
            if (_scope is not null && _version == services.Version)
            {
                return _scope.ServiceProvider;
            }

            _scope?.Dispose();
            _scope = services.CreateScope(fallbackProvider);
            _version = services.Version;
            return _scope.ServiceProvider;
        }

        public void Dispose()
        {
            _scope?.Dispose();
            _scope = null;
            _version = -1;
        }
    }

    private sealed class DynamicFactoryContainerProvider : IContainerProvider
    {
        private static readonly AsyncLocal<IContainerProvider?> Current = new();
        private static readonly AsyncLocal<IServiceProvider?> CurrentFallback = new();

        public static IDisposable Use(IContainerProvider containerProvider, IServiceProvider fallbackProvider)
        {
            var previous = Current.Value;
            var previousFallback = CurrentFallback.Value;
            Current.Value = containerProvider;
            CurrentFallback.Value = fallbackProvider;
            return new DynamicFactoryScope(previous, previousFallback);
        }

        public static object CreateInstance(IServiceProvider dynamicProvider, Type implementationType)
        {
            return ActivatorUtilities.CreateInstance(
                new CompositeFallbackServiceProvider(dynamicProvider, CurrentFallback.Value ?? EmptyServiceProvider.Instance),
                implementationType);
        }

        public object Resolve(Type type) => Active.Resolve(type);

        public object Resolve(Type type, string? name) => Active.Resolve(type, name);

        public T Resolve<T>() => Active.Resolve<T>();

        public T Resolve<T>(string name) => Active.Resolve<T>(name);

        public bool IsRegistered(Type type) => Active.IsRegistered(type);

        public bool IsRegistered(Type type, string name) => Active.IsRegistered(type, name);

        public IContainerProvider CreateScope() => Active.CreateScope();

        public IContainerProvider CreateScope(string? moduleName) => Active.CreateScope(moduleName);

        public static IContainerProvider Active =>
            Current.Value ?? throw new InvalidOperationException("Dynamic service factory is not executing inside a container context.");

        private sealed class DynamicFactoryScope : IDisposable
        {
            private readonly IContainerProvider? _previous;
            private readonly IServiceProvider? _previousFallback;
            private bool _disposed;

            public DynamicFactoryScope(IContainerProvider? previous, IServiceProvider? previousFallback)
            {
                _previous = previous;
                _previousFallback = previousFallback;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Current.Value = _previous;
                CurrentFallback.Value = _previousFallback;
            }
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider, IServiceProviderIsService, IKeyedServiceProvider
    {
        public static readonly EmptyServiceProvider Instance = new();

        private EmptyServiceProvider()
        {
        }

        public object? GetService(Type serviceType) => null;

        public bool IsService(Type serviceType) => false;

        public object? GetKeyedService(Type serviceType, object? serviceKey) => null;

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        {
            throw new InvalidOperationException($"No keyed service for type '{serviceType.FullName}' with key '{serviceKey}' has been registered.");
        }
    }

    private sealed class CompositeFallbackServiceProvider : IServiceProvider, IServiceProviderIsService, IKeyedServiceProvider
    {
        private readonly IServiceProvider _primary;
        private readonly IServiceProvider _fallback;

        public CompositeFallbackServiceProvider(IServiceProvider primary, IServiceProvider fallback)
        {
            _primary = primary;
            _fallback = fallback;
        }

        public object? GetService(Type serviceType)
        {
            return _primary.GetService(serviceType) ?? _fallback.GetService(serviceType);
        }

        public bool IsService(Type serviceType)
        {
            return (_primary is IServiceProviderIsService primaryIsService && primaryIsService.IsService(serviceType)) ||
                   (_fallback is IServiceProviderIsService fallbackIsService && fallbackIsService.IsService(serviceType));
        }

        public object? GetKeyedService(Type serviceType, object? serviceKey)
        {
            if (_primary is IKeyedServiceProvider primaryKeyed)
            {
                var service = primaryKeyed.GetKeyedService(serviceType, serviceKey);
                if (service is not null)
                {
                    return service;
                }
            }

            return _fallback is IKeyedServiceProvider fallbackKeyed
                ? fallbackKeyed.GetKeyedService(serviceType, serviceKey)
                : null;
        }

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        {
            return GetKeyedService(serviceType, serviceKey) ??
                   throw new InvalidOperationException($"No keyed service for type '{serviceType.FullName}' with key '{serviceKey}' has been registered.");
        }
    }

}
