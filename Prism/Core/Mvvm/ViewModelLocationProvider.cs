using System.Collections.Concurrent;
using System.Reflection;
using Avalonia.Controls;

namespace Prism.Mvvm;

public static class ViewModelLocationProvider
{
    private const int DefaultMaxCacheEntries = 2048;
    private const int DefaultMaxScopeInheritanceDepth = 64;
    private const string GlobalKeyPrefix = "g:";
    private static readonly ConcurrentDictionary<Type, Func<object>> Factories = new();
    private static readonly ConcurrentDictionary<string, Type> ViewModelTypes = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, Assembly> ViewModelAssemblies = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, Lazy<Assembly>> LazyViewModelAssemblies = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, ViewModelTypeCacheEntry> TypeCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, StrongViewModelTypeCacheEntry> StrongTypeCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> PersistentCacheKeys = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Guid, ScopeFactoryRegistration> ScopeFactories = new();
    private static readonly ConcurrentDictionary<Guid, ScopeUsageCounter> ScopeUsages = new();
    private static readonly ConcurrentDictionary<string, List<WildcardRegistration>> WildcardRegistrationsByPrefix = new(StringComparer.Ordinal);
    private static readonly AsyncLocal<Func<object?, Type, object>?> ScopedFactory = new();
    private static readonly AsyncLocal<Guid?> CurrentScopeId = new();
    private static readonly AsyncLocal<string?> CurrentModuleName = new();
    private static Func<Type, Type?> _resolver = DefaultResolver;
    private static Func<ViewModelResolutionContext, IEnumerable<string>> _candidateProvider = DefaultCandidateProvider;
    private static string? _defaultViewNamespace;
    private static string? _defaultViewModelNamespace;
    private static Func<object?, Type, object>? _factory;
    private static Func<object?, Type, object?>? _designTimeFactory;
    private static IViewModelLocationLogger _logger = NullViewModelLocationLogger.Instance;
    private static int _cacheCleanupCounter;
    private static int _maxCacheEntries = DefaultMaxCacheEntries;
    private static TimeSpan _cacheEntryLifetime = TimeSpan.FromMinutes(30);
    private static bool _useStrongTypeCache;
    private static ViewModelLocationAotOptimizeMode _aotOptimizeMode;
    private static int _maxScopeInheritanceDepth = DefaultMaxScopeInheritanceDepth;

    public static int MaxCacheEntries
    {
        get => _maxCacheEntries;
        set
        {
            EnsureCanWrite();
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Max cache entries must be greater than zero.");
            }

            _maxCacheEntries = value;
            CleanupCache(force: true);
        }
    }

    public static TimeSpan CacheEntryLifetime
    {
        get => _cacheEntryLifetime;
        set
        {
            EnsureCanWrite();
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Cache entry lifetime must be greater than zero.");
            }

            _cacheEntryLifetime = value;
            CleanupCache(force: true);
        }
    }

    public static TimeSpan? StrongCacheLifetime { get; set; }

    public static ViewModelLocationAotOptimizeMode AotOptimizeMode
    {
        get => _aotOptimizeMode;
        set
        {
            EnsureCanWrite();
            _aotOptimizeMode = value;
            if (value == ViewModelLocationAotOptimizeMode.Enabled)
            {
                TypeCache.Clear();
            }
        }
    }

    public static bool UseStrongTypeCache
    {
        get => _useStrongTypeCache;
        set
        {
            EnsureCanWrite();
            _useStrongTypeCache = value;
        }
    }

    public static bool IsReadOnly { get; private set; }

    public static int MaxScopeInheritanceDepth
    {
        get => _maxScopeInheritanceDepth;
        set
        {
            EnsureCanWrite();
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Scope inheritance depth must be greater than zero.");
            }

            _maxScopeInheritanceDepth = value;
        }
    }

    public static void Register<TView, TViewModel>()
        where TViewModel : class
    {
        Register(typeof(TView), typeof(TViewModel));
    }

    public static void Register(Type viewType, Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewType);
        RegisterCore(GetViewRegistrationKeys(viewType), viewModelType, CurrentModuleName.Value);
    }

    public static void Register<TView>(Func<object> factory)
    {
        EnsureCanWrite();
        Factories[typeof(TView)] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public static void Register(string viewTypeName, Type viewModelType)
    {
        Register(viewTypeName, viewModelType, CurrentModuleName.Value);
    }

    public static void Register(string viewTypeName, Type viewModelType, string? moduleName)
    {
        if (string.IsNullOrWhiteSpace(viewTypeName))
        {
            throw new ArgumentException("View type name cannot be empty.", nameof(viewTypeName));
        }

        RegisterCore(new[] { viewTypeName }, viewModelType, moduleName);
    }

    public static bool Unregister(Type viewType)
    {
        ArgumentNullException.ThrowIfNull(viewType);
        EnsureCanWrite();
        var removed = Factories.TryRemove(viewType, out _);
        foreach (var key in GetViewRegistrationKeys(viewType))
        {
            removed |= RemoveRegistration(GetModuleKey(CurrentModuleName.Value, key));
            removed |= RemoveRegistration(GetModuleKey(null, key));
        }

        ClearCache();
        return removed;
    }

    public static bool Unregister(string key, string? moduleName = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("ViewModel registration key cannot be empty.", nameof(key));
        }

        EnsureCanWrite();
        var removed = RemoveRegistration(GetModuleKey(moduleName, key));
        ClearCache();
        return removed;
    }

    public static void ClearModuleRegistrations(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new ArgumentException("Module name cannot be empty.", nameof(moduleName));
        }

        EnsureCanWrite();
        var registrationPrefix = GetModuleKeyPrefix(moduleName);
        foreach (var key in ViewModelTypes.Keys.Where(key => key.StartsWith(registrationPrefix, StringComparison.Ordinal)))
        {
            RemoveRegistration(key);
        }

        var cachePrefix = GetCacheModulePrefix(moduleName);
        foreach (var key in StrongTypeCache.Keys.Where(key => key.StartsWith(cachePrefix, StringComparison.Ordinal)))
        {
            StrongTypeCache.TryRemove(key, out _);
        }

        foreach (var key in PersistentCacheKeys.Keys.Where(key => key.StartsWith(cachePrefix, StringComparison.Ordinal)))
        {
            PersistentCacheKeys.TryRemove(key, out _);
        }

        ClearCache();
    }

    public static IReadOnlyDictionary<string, Type> GetGlobalAllRegistrations()
    {
        return ViewModelTypes
            .Where(item => item.Key.StartsWith(GlobalKeyPrefix, StringComparison.Ordinal))
            .ToDictionary(item => item.Key[GlobalKeyPrefix.Length..], item => item.Value, StringComparer.Ordinal);
    }

    public static IReadOnlyDictionary<string, Type> GetModuleAllRegistrations(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new ArgumentException("Module name cannot be empty.", nameof(moduleName));
        }

        var prefix = GetModuleKeyPrefix(moduleName);
        return ViewModelTypes
            .Where(item => item.Key.StartsWith(prefix, StringComparison.Ordinal))
            .ToDictionary(item => item.Key[prefix.Length..], item => item.Value, StringComparer.Ordinal);
    }

    public static IReadOnlyList<ViewModelLocationScopeNode> GetScopeTree()
    {
        var registrations = ScopeFactories.ToArray();
        var childrenByParent = registrations
            .GroupBy(item => item.Value.ParentScopeId ?? Guid.Empty)
            .ToDictionary(group => group.Key, group => group.ToArray());

        return BuildScopeNodes(Guid.Empty, childrenByParent, new HashSet<Guid>(), 0);
    }

    public static IReadOnlyList<Guid> GetScopeChain(Guid scopeId)
    {
        var chain = new List<Guid>();
        var visited = new HashSet<Guid>();
        var current = scopeId;
        var depth = 0;
        while (depth++ < MaxScopeInheritanceDepth && visited.Add(current))
        {
            chain.Add(current);
            if (!ScopeFactories.TryGetValue(current, out var registration) ||
                registration.ParentScopeId is not { } parentScopeId)
            {
                break;
            }

            current = parentScopeId;
        }

        return chain;
    }

    public static IDisposable TrackScopeUsage(Guid scopeId)
    {
        if (!ScopeFactories.ContainsKey(scopeId))
        {
            return new DisposableAction(static () => { });
        }

        var counter = ScopeUsages.GetOrAdd(scopeId, static _ => new ScopeUsageCounter());
        counter.Increment();
        return new DisposableAction(() =>
        {
            if (counter.Decrement() <= 0)
            {
                ScopeUsages.TryRemove(scopeId, out _);
            }
        });
    }

    public static void RegisterAlias(string alias, Type viewModelType)
    {
        Register(alias, viewModelType);
    }

    public static void RegisterNavigationAlias(string routeName, Type viewModelType)
    {
        Register(routeName, viewModelType);
    }

    public static void RegisterViewModels(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        RegisterViewModelAssembly(assembly);
        EnsureCanWrite();

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || !type.Name.EndsWith("ViewModel", StringComparison.Ordinal))
            {
                continue;
            }

            var baseName = TrimGenericArity(type.Name[..^"ViewModel".Length]);
            RegisterCore(new[]
            {
                type.FullName,
                type.Name,
                baseName,
                baseName + "View",
                baseName + "Page"
            }, type, CurrentModuleName.Value);
        }
    }

    public static void RegisterViewModelAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        foreach (var assembly in assemblies)
        {
            RegisterViewModelAssembly(assembly);
        }
    }

    public static void RegisterViewModelAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        EnsureCanWrite();
        ViewModelAssemblies[assembly.FullName ?? assembly.GetName().Name ?? assembly.Location] = assembly;
        ClearCache();
    }

    public static void RegisterViewModelAssembly(string key, Func<Assembly> assemblyFactory)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Assembly registration key cannot be empty.", nameof(key));
        }

        ArgumentNullException.ThrowIfNull(assemblyFactory);
        EnsureCanWrite();
        LazyViewModelAssemblies[key] = new Lazy<Assembly>(assemblyFactory, LazyThreadSafetyMode.ExecutionAndPublication);
        ClearCache();
    }

    public static void SetDefaultViewTypeToViewModelTypeResolver(Func<Type, Type?> resolver)
    {
        EnsureCanWrite();
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        ClearCache();
    }

    public static void SetDefaultViewModelCandidateProvider(Func<ViewModelResolutionContext, IEnumerable<string>> candidateProvider)
    {
        EnsureCanWrite();
        _candidateProvider = candidateProvider ?? throw new ArgumentNullException(nameof(candidateProvider));
        ClearCache();
    }

    public static void SetDefaultNamespace(string viewNamespace, string viewModelNamespace)
    {
        if (string.IsNullOrWhiteSpace(viewNamespace))
        {
            throw new ArgumentException("View namespace cannot be empty.", nameof(viewNamespace));
        }

        if (string.IsNullOrWhiteSpace(viewModelNamespace))
        {
            throw new ArgumentException("ViewModel namespace cannot be empty.", nameof(viewModelNamespace));
        }

        EnsureCanWrite();
        _defaultViewNamespace = viewNamespace.TrimEnd('.');
        _defaultViewModelNamespace = viewModelNamespace.TrimEnd('.');
        ClearCache();
    }

    public static void ClearDefaultNamespace()
    {
        EnsureCanWrite();
        _defaultViewNamespace = null;
        _defaultViewModelNamespace = null;
        ClearCache();
    }

    public static void UseStrongCacheFor<TView>(string? routeName = null, string? moduleName = null)
    {
        UseStrongCacheFor(typeof(TView), routeName, moduleName);
    }

    public static void UseStrongCacheFor(Type viewType, string? routeName = null, string? moduleName = null)
    {
        ArgumentNullException.ThrowIfNull(viewType);
        EnsureCanWrite();
        PersistentCacheKeys[CreateCacheKey(viewType, routeName, moduleName ?? CurrentModuleName.Value)] = 0;
    }

    public static bool ClearStrongCacheFor(Type viewType, string? routeName = null, string? moduleName = null)
    {
        ArgumentNullException.ThrowIfNull(viewType);
        EnsureCanWrite();
        var key = CreateCacheKey(viewType, routeName, moduleName ?? CurrentModuleName.Value);
        PersistentCacheKeys.TryRemove(key, out _);
        return StrongTypeCache.TryRemove(key, out _);
    }

    public static bool DowngradeStrongCacheFor(Type viewType, string? routeName = null, string? moduleName = null)
    {
        ArgumentNullException.ThrowIfNull(viewType);
        EnsureCanWrite();
        var key = CreateCacheKey(viewType, routeName, moduleName ?? CurrentModuleName.Value);
        PersistentCacheKeys.TryRemove(key, out _);
        if (!StrongTypeCache.TryRemove(key, out var strongEntry))
        {
            return false;
        }

        TypeCache[key] = new ViewModelTypeCacheEntry(strongEntry.ViewModelType);
        return true;
    }

    public static void DowngradeAllStrongCaches()
    {
        EnsureCanWrite();
        PersistentCacheKeys.Clear();
        foreach (var item in StrongTypeCache)
        {
            if (StrongTypeCache.TryRemove(item.Key, out var strongEntry))
            {
                TypeCache[item.Key] = new ViewModelTypeCacheEntry(strongEntry.ViewModelType);
            }
        }
    }

    public static void SetDefaultViewModelFactory(Func<Type, object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        SetDefaultViewModelFactory((_, type) => factory(type));
    }

    public static void SetDefaultViewModelFactory(Func<object?, Type, object> factory)
    {
        EnsureCanWrite();
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public static void SetDesignTimeViewModelFactory(Func<object?, Type, object?> factory)
    {
        EnsureCanWrite();
        _designTimeFactory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public static void SetLogger(IViewModelLocationLogger logger)
    {
        _logger = logger ?? NullViewModelLocationLogger.Instance;
    }

    public static IDisposable UseScopedViewModelFactory(Func<Type, object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return UseScopedViewModelFactory((_, type) => factory(type));
    }

    public static IDisposable UseScopedViewModelFactory(Func<object?, Type, object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var previous = ScopedFactory.Value;
        ScopedFactory.Value = factory;
        return new DisposableAction(() => ScopedFactory.Value = previous);
    }

    public static ViewModelLocationScope CreateScopedViewModelFactory(Func<object?, Type, object> factory)
    {
        return CreateScopedViewModelFactory(factory, CurrentModuleName.Value, CurrentScopeId.Value);
    }

    public static ViewModelLocationScope CreateScopedViewModelFactory(Func<object?, Type, object> factory, string? moduleName)
    {
        return CreateScopedViewModelFactory(factory, moduleName, CurrentScopeId.Value);
    }

    public static ViewModelLocationScope CreateScopedViewModelFactory(Func<object?, Type, object> factory, string? moduleName, Guid? parentScopeId)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var id = Guid.NewGuid();
        ScopeFactories[id] = new ScopeFactoryRegistration(factory, parentScopeId, moduleName);
        var scope = new ViewModelLocationScope(id, parentScopeId, moduleName);
        ScopeCreated?.Invoke(null, new ViewModelLocationScopeChangedEventArgs(id, parentScopeId, moduleName));
        return scope;
    }

    internal static void ReleaseScopedViewModelFactory(Guid scopeId)
    {
        ScopeFactories.TryRemove(scopeId, out var registration);
        var activeUseCount = ScopeUsages.TryGetValue(scopeId, out var usageCounter) ? usageCounter.Count : 0;
        ScopeUsages.TryRemove(scopeId, out _);
        if (CurrentScopeId.Value == scopeId)
        {
            CurrentScopeId.Value = null;
        }

        ScopeDisposed?.Invoke(null, new ViewModelLocationScopeChangedEventArgs(
            scopeId,
            registration?.ParentScopeId,
            registration?.ModuleName,
            activeUseCount));
    }

    public static event EventHandler<ViewModelLocationScopeChangedEventArgs>? ScopeCreated;

    public static event EventHandler<ViewModelLocationScopeChangedEventArgs>? ScopeDisposed;

    public static Guid? GetParentScope(Guid scopeId)
    {
        return ScopeFactories.TryGetValue(scopeId, out var registration)
            ? registration.ParentScopeId
            : null;
    }

    public static IDisposable BeginModuleContext(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new ArgumentException("Module name cannot be empty.", nameof(moduleName));
        }

        var previous = CurrentModuleName.Value;
        CurrentModuleName.Value = moduleName;
        return new DisposableAction(() => CurrentModuleName.Value = previous);
    }

    public static ViewModelLocationContext CaptureContext()
    {
        return new ViewModelLocationContext(ScopedFactory.Value, CurrentScopeId.Value, CurrentModuleName.Value);
    }

    public static IDisposable UseContext(ViewModelLocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var previousFactory = ScopedFactory.Value;
        var previousScopeId = CurrentScopeId.Value;
        var previousModuleName = CurrentModuleName.Value;
        ScopedFactory.Value = context.ScopedFactory;
        CurrentScopeId.Value = context.ScopeId;
        CurrentModuleName.Value = context.ModuleName;
        return new DisposableAction(() =>
        {
            ScopedFactory.Value = previousFactory;
            CurrentScopeId.Value = previousScopeId;
            CurrentModuleName.Value = previousModuleName;
        });
    }

    public static Action Capture(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var context = CaptureContext();
        return () =>
        {
            using var _ = UseContext(context);
            action();
        };
    }

    public static Func<Task> Capture(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var context = CaptureContext();
        return async () =>
        {
            using var _ = UseContext(context);
            await action().ConfigureAwait(false);
        };
    }

    public static object? AutoWireViewModelChanged(object view)
    {
        return AutoWireViewModelChanged(view, null);
    }

    public static object? AutoWireViewModelChanged(object view, string? routeName)
    {
        ArgumentNullException.ThrowIfNull(view);

        if (Factories.TryGetValue(view.GetType(), out var registeredFactory))
        {
            return registeredFactory();
        }

        var result = ResolveViewModelType(view.GetType(), routeName);
        if (result.ViewModelType is null)
        {
            if (IsInDesignMode)
            {
                return new DesignTimeViewModel(view.GetType(), null);
            }

            var exception = CreateResolutionException(view.GetType(), routeName, result, null);
            _logger.ResolutionFailed(view.GetType(), exception);
            throw exception;
        }

        return CreateViewModel(view, result.ViewModelType, routeName, result);
    }

    public static object? AutoWireViewModelForNavigation(object view, string routeName)
    {
        return AutoWireViewModelChanged(view, routeName);
    }

    public static Type? ResolveViewModelTypeForNavigation(Type viewType, string routeName)
    {
        return ResolveViewModelType(viewType, routeName).ViewModelType;
    }

    public static void Freeze()
    {
        IsReadOnly = true;
    }

    public static void Unfreeze()
    {
        IsReadOnly = false;
    }

    public static void Clear()
    {
        EnsureCanWrite();
        Factories.Clear();
        ViewModelTypes.Clear();
        ViewModelAssemblies.Clear();
        LazyViewModelAssemblies.Clear();
        ScopeFactories.Clear();
        ScopeUsages.Clear();
        WildcardRegistrationsByPrefix.Clear();
        StrongTypeCache.Clear();
        PersistentCacheKeys.Clear();
        ClearCache();
    }

    public static void Reset()
    {
        IsReadOnly = false;
        Factories.Clear();
        ViewModelTypes.Clear();
        ViewModelAssemblies.Clear();
        LazyViewModelAssemblies.Clear();
        ScopeFactories.Clear();
        ScopeUsages.Clear();
        WildcardRegistrationsByPrefix.Clear();
        TypeCache.Clear();
        StrongTypeCache.Clear();
        PersistentCacheKeys.Clear();
        _resolver = DefaultResolver;
        _candidateProvider = DefaultCandidateProvider;
        _defaultViewNamespace = null;
        _defaultViewModelNamespace = null;
        UseStrongTypeCache = false;
        AotOptimizeMode = ViewModelLocationAotOptimizeMode.Disabled;
        _maxScopeInheritanceDepth = DefaultMaxScopeInheritanceDepth;
        _factory = null;
        _designTimeFactory = null;
        ScopedFactory.Value = null;
        CurrentScopeId.Value = null;
        CurrentModuleName.Value = null;
    }

    private static object? CreateViewModel(object view, Type viewModelType, string? routeName, ViewModelResolutionResult resolutionResult)
    {
        var factory = GetCurrentFactory();
        if (factory is not null)
        {
            try
            {
                var viewModel = factory(view, viewModelType);
                _logger.ResolutionSucceeded(view.GetType(), viewModelType, resolutionResult.MatchingRule);
                return viewModel;
            }
            catch (Exception ex)
            {
                var exception = CreateResolutionException(view.GetType(), routeName, resolutionResult, ex);
                _logger.ResolutionFailed(view.GetType(), exception);
                throw exception;
            }
        }

        if (IsInDesignMode)
        {
            return _designTimeFactory?.Invoke(view, viewModelType) ??
                   new DesignTimeViewModel(view.GetType(), viewModelType);
        }

        var missingFactoryException = CreateResolutionException(view.GetType(), routeName, resolutionResult, null);
        _logger.ResolutionFailed(view.GetType(), missingFactoryException);
        throw missingFactoryException;
    }

    private static Func<object?, Type, object>? GetCurrentFactory()
    {
        if (ScopedFactory.Value is { } scopedFactory)
        {
            return scopedFactory;
        }

        if (CurrentScopeId.Value is { } scopeId &&
            TryGetScopeFactory(scopeId, out var registeredScopeFactory))
        {
            return registeredScopeFactory;
        }

        return _factory;
    }

    internal static Guid? GetScopeId(ViewModelLocationScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return scope.Id;
    }

    private static bool TryGetScopeFactory(Guid scopeId, out Func<object?, Type, object> factory)
    {
        var visited = new HashSet<Guid>();
        var current = scopeId;
        var depth = 0;
        while (depth < MaxScopeInheritanceDepth && visited.Add(current))
        {
            if (!ScopeFactories.TryGetValue(current, out var registration))
            {
                break;
            }

            factory = registration.Factory;
            return true;
        }

        factory = null!;
        return false;
    }

    private static ViewModelResolutionResult ResolveViewModelType(Type viewType, string? routeName)
    {
        CleanupCacheIfNeeded();
        var moduleName = CurrentModuleName.Value;
        var cacheKey = CreateCacheKey(viewType, routeName, moduleName);
        if (TryGetCachedType(cacheKey, out var cachedType, out var cacheRule))
        {
            return new ViewModelResolutionResult(cachedType, Array.Empty<string>(), Array.Empty<string>(), cacheRule);
        }

        var candidates = new List<string>();
        var searchedAssemblies = new List<string>();
        var registeredType = ResolveRegisteredType(viewType, routeName, moduleName, candidates);
        if (registeredType is not null)
        {
            SetCachedType(cacheKey, registeredType);
            return new ViewModelResolutionResult(registeredType, candidates, searchedAssemblies, "Explicit registration");
        }

        var resolvedType = _resolver(viewType);
        if (resolvedType is not null)
        {
            SetCachedType(cacheKey, resolvedType);
            return new ViewModelResolutionResult(resolvedType, candidates, searchedAssemblies, "Custom resolver");
        }

        foreach (var baseType in EnumerateViewTypeHierarchy(viewType))
        {
            var context = new ViewModelResolutionContext(baseType, routeName, moduleName);
            foreach (var candidate in _candidateProvider(context).Where(static item => !string.IsNullOrWhiteSpace(item)))
            {
                AddCandidate(candidates, candidate);
            }

            var assemblies = GetSearchAssemblies(baseType);
            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.FullName ?? assembly.GetName().Name ?? assembly.Location;
                if (!searchedAssemblies.Contains(assemblyName, StringComparer.Ordinal))
                {
                    searchedAssemblies.Add(assemblyName);
                }

                foreach (var candidate in candidates)
                {
                    var type = FindType(assembly, candidate);
                    if (type is not null)
                    {
                        SetCachedType(cacheKey, type);
                        return new ViewModelResolutionResult(type, candidates, searchedAssemblies, "Convention");
                    }
                }
            }
        }

        return new ViewModelResolutionResult(null, candidates, searchedAssemblies, "Convention");
    }

    private static Type? ResolveRegisteredType(Type viewType, string? routeName, string? moduleName, List<string> candidates)
    {
        foreach (var candidateType in EnumerateViewTypeHierarchy(viewType))
        {
            foreach (var key in GetRegistrationLookupKeys(candidateType, routeName))
            {
                AddCandidate(candidates, key);
                if (TryGetRegisteredType(key, moduleName, out var registeredType))
                {
                    return registeredType;
                }
            }
        }

        return null;
    }

    private static bool TryGetRegisteredType(string key, string? moduleName, out Type type)
    {
        if (!string.IsNullOrWhiteSpace(moduleName) &&
            ViewModelTypes.TryGetValue(GetModuleKey(moduleName, key), out type!))
        {
            return true;
        }

        if (ViewModelTypes.TryGetValue(GetModuleKey(null, key), out type!))
        {
            return true;
        }

        return TryGetWildcardRegisteredType(key, moduleName, out type!);
    }

    private static bool TryGetWildcardRegisteredType(string key, string? moduleName, out Type type)
    {
        if (!string.IsNullOrWhiteSpace(moduleName))
        {
            var modulePrefix = GetModuleKeyPrefix(moduleName);
            var indexedKey = modulePrefix + key;
            if (TryGetWildcardRegisteredTypeFromIndex(indexedKey, key, out type))
            {
                return true;
            }
        }

        return TryGetWildcardRegisteredTypeFromIndex(GlobalKeyPrefix + key, key, out type);
    }

    private static bool TryGetWildcardRegisteredTypeFromIndex(string indexedKey, string key, out Type type)
    {
        foreach (var prefix in GetWildcardPrefixCandidates(indexedKey))
        {
            if (!WildcardRegistrationsByPrefix.TryGetValue(prefix, out var registrations))
            {
                continue;
            }

            lock (registrations)
            {
                foreach (var registration in registrations)
                {
                    if (IsWildcardMatch(key, registration.Pattern))
                    {
                        type = registration.ViewModelType;
                        return true;
                    }
                }
            }
        }

        type = null!;
        return false;
    }

    private static IEnumerable<Type> EnumerateViewTypeHierarchy(Type viewType)
    {
        var visited = new HashSet<Type>();
        for (var current = viewType; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (visited.Add(current))
            {
                yield return current;
            }

            if (current.IsGenericType)
            {
                var genericDefinition = current.GetGenericTypeDefinition();
                if (visited.Add(genericDefinition))
                {
                    yield return genericDefinition;
                }
            }

            foreach (var interfaceType in current.GetInterfaces())
            {
                if (visited.Add(interfaceType))
                {
                    yield return interfaceType;
                }

                if (interfaceType.IsGenericType)
                {
                    var genericDefinition = interfaceType.GetGenericTypeDefinition();
                    if (visited.Add(genericDefinition))
                    {
                        yield return genericDefinition;
                    }
                }
            }
        }
    }

    private static IEnumerable<Assembly> GetSearchAssemblies(Type viewType)
    {
        yield return viewType.Assembly;

        foreach (var assembly in ViewModelAssemblies.Values)
        {
            if (!ReferenceEquals(assembly, viewType.Assembly))
            {
                yield return assembly;
            }
        }

        foreach (var lazyAssembly in LazyViewModelAssemblies.Values)
        {
            var assembly = lazyAssembly.Value;
            if (!ReferenceEquals(assembly, viewType.Assembly))
            {
                yield return assembly;
            }
        }
    }

    private static Type? DefaultResolver(Type viewType)
    {
        var candidates = DefaultCandidateProvider(new ViewModelResolutionContext(viewType, null, CurrentModuleName.Value));
        foreach (var assembly in GetSearchAssemblies(viewType))
        {
            foreach (var candidate in candidates)
            {
                var type = FindType(assembly, candidate);
                if (type is not null)
                {
                    return type;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> DefaultCandidateProvider(ViewModelResolutionContext context)
    {
        var viewType = context.ViewType;
        var fullName = viewType.FullName;
        if (fullName is null)
        {
            return Array.Empty<string>();
        }

        var viewName = TrimGenericArity(viewType.Name);
        var viewModelName = viewName.EndsWith("View", StringComparison.Ordinal)
            ? viewName[..^"View".Length] + "ViewModel"
            : viewName + "ViewModel";

        var viewModelsFullName = fullName.Replace(".Views.", ".ViewModels.", StringComparison.Ordinal);
        var candidates = new List<string>();
        if (_defaultViewNamespace is not null &&
            _defaultViewModelNamespace is not null &&
            fullName.StartsWith(_defaultViewNamespace + ".", StringComparison.Ordinal))
        {
            var relativeName = fullName[(_defaultViewNamespace.Length + 1)..];
            var relativeViewModelName = relativeName.EndsWith("View", StringComparison.Ordinal)
                ? relativeName[..^"View".Length] + "ViewModel"
                : relativeName + "ViewModel";
            candidates.Add(_defaultViewModelNamespace + "." + relativeViewModelName);
        }

        candidates.AddRange(new[]
        {
            viewModelsFullName + "Model",
            fullName + "Model",
            viewModelsFullName.Replace(viewName, viewModelName, StringComparison.Ordinal),
            $"{viewType.Namespace}.ViewModels.{viewModelName}"
        });

        return candidates;
    }

    private static IEnumerable<string> GetViewRegistrationKeys(Type viewType)
    {
        var name = TrimGenericArity(viewType.Name);
        var keys = new List<string?>
        {
            viewType.ToString(),
            viewType.FullName,
            viewType.AssemblyQualifiedName,
            name
        };

        if (viewType.IsGenericType)
        {
            var genericDefinition = viewType.GetGenericTypeDefinition();
            keys.Add(genericDefinition.ToString());
            keys.Add(genericDefinition.FullName);
            keys.Add(genericDefinition.AssemblyQualifiedName);
            keys.Add(TrimGenericArity(genericDefinition.Name));
        }

        if (name.EndsWith("View", StringComparison.Ordinal))
        {
            keys.Add(name[..^"View".Length]);
        }

        return keys.Where(static key => !string.IsNullOrWhiteSpace(key))!;
    }

    private static IEnumerable<string> GetRegistrationLookupKeys(Type viewType, string? routeName)
    {
        if (!string.IsNullOrWhiteSpace(routeName))
        {
            yield return routeName;
            yield return routeName.Trim('/');
            yield return routeName.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? routeName;
        }

        foreach (var key in GetViewRegistrationKeys(viewType))
        {
            yield return key;
        }
    }

    private static void RegisterCore(IEnumerable<string?> keys, Type viewModelType, string? moduleName)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);
        EnsureCanWrite();
        if (!viewModelType.IsClass || viewModelType.IsAbstract)
        {
            throw new ArgumentException($"ViewModel type must be a concrete class. Type: {viewModelType.FullName}", nameof(viewModelType));
        }

        foreach (var key in keys.Where(static item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.Ordinal))
        {
            AddRegistration(GetModuleKey(moduleName, key!), viewModelType);
        }

        RegisterViewModelAssembly(viewModelType.Assembly);
    }

    private static void AddRegistration(string fullKey, Type viewModelType)
    {
        ViewModelTypes[fullKey] = viewModelType;
        if (ExtractRegistrationKey(fullKey) is { } key &&
            IsWildcardPattern(key))
        {
            var prefix = GetWildcardPrefix(fullKey);
            var registrations = WildcardRegistrationsByPrefix.GetOrAdd(prefix, static _ => new List<WildcardRegistration>());
            lock (registrations)
            {
                registrations.RemoveAll(item => string.Equals(item.FullKey, fullKey, StringComparison.Ordinal));
                registrations.Add(new WildcardRegistration(fullKey, key, viewModelType));
            }
        }
    }

    private static bool RemoveRegistration(string fullKey)
    {
        var removed = ViewModelTypes.TryRemove(fullKey, out _);
        if (removed && ExtractRegistrationKey(fullKey) is { } key && IsWildcardPattern(key))
        {
            var prefix = GetWildcardPrefix(fullKey);
            if (WildcardRegistrationsByPrefix.TryGetValue(prefix, out var registrations))
            {
                lock (registrations)
                {
                    registrations.RemoveAll(item => string.Equals(item.FullKey, fullKey, StringComparison.Ordinal));
                    if (registrations.Count == 0)
                    {
                        WildcardRegistrationsByPrefix.TryRemove(prefix, out _);
                    }
                }
            }
        }

        return removed;
    }

    private static Type? FindType(Assembly assembly, string candidate)
    {
        return assembly.GetType(candidate, throwOnError: false) ??
               assembly.GetType(candidate.Replace('.', '+'), throwOnError: false);
    }

    private static bool TryGetCachedType(string cacheKey, out Type type, out string cacheRule)
    {
        if (StrongTypeCache.TryGetValue(cacheKey, out var strongEntry))
        {
            if (strongEntry.IsExpired(DateTimeOffset.UtcNow))
            {
                if (StrongTypeCache.TryRemove(cacheKey, out var removedEntry))
                {
                    TypeCache[cacheKey] = new ViewModelTypeCacheEntry(removedEntry.ViewModelType);
                }
            }
            else
            {
                type = strongEntry.ViewModelType;
                cacheRule = "Strong cache";
                _logger.CacheHit(type, type, cacheRule);
                return true;
            }
        }

        if (TypeCache.TryGetValue(cacheKey, out var cached) && cached.TryGet(out type!, CacheEntryLifetime))
        {
            cacheRule = "Weak cache";
            _logger.CacheHit(type, type, "Weak cache");
            return true;
        }

        type = null!;
        cacheRule = string.Empty;
        return false;
    }

    private static void SetCachedType(string cacheKey, Type viewModelType)
    {
        if (AotOptimizeMode == ViewModelLocationAotOptimizeMode.Enabled ||
            UseStrongTypeCache ||
            PersistentCacheKeys.ContainsKey(cacheKey))
        {
            StrongTypeCache[cacheKey] = new StrongViewModelTypeCacheEntry(viewModelType, StrongCacheLifetime);
            TypeCache.TryRemove(cacheKey, out _);
            return;
        }

        TypeCache[cacheKey] = new ViewModelTypeCacheEntry(viewModelType);
    }

    private static string GetModuleKey(string? moduleName, string key)
    {
        return string.IsNullOrWhiteSpace(moduleName)
            ? GlobalKeyPrefix + key
            : GetModuleKeyPrefix(moduleName) + key;
    }

    private static string GetModuleKeyPrefix(string moduleName)
    {
        return $"m:{moduleName.Length}:{moduleName}:";
    }

    private static string? ExtractRegistrationKey(string fullKey)
    {
        if (fullKey.StartsWith(GlobalKeyPrefix, StringComparison.Ordinal))
        {
            return fullKey[GlobalKeyPrefix.Length..];
        }

        if (!fullKey.StartsWith("m:", StringComparison.Ordinal))
        {
            return null;
        }

        var lengthStart = 2;
        var lengthEnd = fullKey.IndexOf(':', lengthStart);
        if (lengthEnd < 0 ||
            !int.TryParse(fullKey[lengthStart..lengthEnd], out var moduleNameLength))
        {
            return null;
        }

        var keyStart = lengthEnd + 1 + moduleNameLength + 1;
        return keyStart <= fullKey.Length ? fullKey[keyStart..] : null;
    }

    private static string CreateCacheKey(Type viewType, string? routeName, string? moduleName)
    {
        var modulePart = moduleName is null ? "g" : GetCacheModulePrefix(moduleName).TrimEnd('|');
        var route = routeName ?? "<default>";
        var typeName = viewType.AssemblyQualifiedName ?? viewType.FullName ?? viewType.Name;
        return $"{modulePart}|r:{route.Length}:{route}|t:{typeName.Length}:{typeName}";
    }

    private static string GetCacheModulePrefix(string moduleName)
    {
        return $"m:{moduleName.Length}:{moduleName}|";
    }

    private static bool IsWildcardMatch(string value, string pattern)
    {
        if (!pattern.Contains('*', StringComparison.Ordinal) &&
            !pattern.Contains('?', StringComparison.Ordinal))
        {
            return false;
        }

        return IsWildcardMatchCore(value, pattern);
    }

    private static bool IsWildcardPattern(string value)
    {
        return value.Contains('*', StringComparison.Ordinal) ||
               value.Contains('?', StringComparison.Ordinal);
    }

    private static string GetWildcardPrefix(string fullKey)
    {
        var wildcardIndex = fullKey.IndexOfAny(new[] { '*', '?' });
        return wildcardIndex <= 0 ? string.Empty : fullKey[..wildcardIndex];
    }

    private static IEnumerable<string> GetWildcardPrefixCandidates(string indexedKey)
    {
        for (var length = indexedKey.Length; length >= 0; length--)
        {
            var prefix = indexedKey[..length];
            if (WildcardRegistrationsByPrefix.ContainsKey(prefix))
            {
                yield return prefix;
            }
        }

        if (WildcardRegistrationsByPrefix.ContainsKey(string.Empty))
        {
            yield return string.Empty;
        }
    }

    private static IReadOnlyList<ViewModelLocationScopeNode> BuildScopeNodes(
        Guid parentScopeId,
        IReadOnlyDictionary<Guid, KeyValuePair<Guid, ScopeFactoryRegistration>[]> childrenByParent,
        HashSet<Guid> visited,
        int depth)
    {
        if (depth >= MaxScopeInheritanceDepth ||
            !childrenByParent.TryGetValue(parentScopeId, out var children))
        {
            return Array.Empty<ViewModelLocationScopeNode>();
        }

        var nodes = new List<ViewModelLocationScopeNode>(children.Length);
        foreach (var child in children)
        {
            if (!visited.Add(child.Key))
            {
                continue;
            }

            var activeUseCount = ScopeUsages.TryGetValue(child.Key, out var usageCounter)
                ? usageCounter.Count
                : 0;
            nodes.Add(new ViewModelLocationScopeNode(
                child.Key,
                child.Value.ParentScopeId,
                child.Value.ModuleName,
                activeUseCount,
                BuildScopeNodes(child.Key, childrenByParent, visited, depth + 1)));
        }

        return nodes;
    }

    private static bool IsWildcardMatchCore(string value, string pattern)
    {
        var valueIndex = 0;
        var patternIndex = 0;
        var starIndex = -1;
        var matchIndex = 0;

        while (valueIndex < value.Length)
        {
            if (patternIndex < pattern.Length &&
                (pattern[patternIndex] == '?' || pattern[patternIndex] == value[valueIndex]))
            {
                patternIndex++;
                valueIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                starIndex = patternIndex++;
                matchIndex = valueIndex;
            }
            else if (starIndex >= 0)
            {
                patternIndex = starIndex + 1;
                valueIndex = ++matchIndex;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }

    private static string TrimGenericArity(string name)
    {
        var index = name.IndexOf('`', StringComparison.Ordinal);
        return index >= 0 ? name[..index] : name.Replace('+', '.');
    }

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        if (!candidates.Contains(candidate, StringComparer.Ordinal))
        {
            candidates.Add(candidate);
        }
    }

    private static ViewModelResolutionException CreateResolutionException(
        Type viewType,
        string? routeName,
        ViewModelResolutionResult result,
        Exception? innerException)
    {
        var candidates = result.CandidatePaths.Take(4).ToArray();
        var message = "Failed to resolve ViewModel." +
                      $"{Environment.NewLine}View: {viewType.FullName}" +
                      $"{Environment.NewLine}Route: {routeName ?? "<none>"}" +
                      $"{Environment.NewLine}Module: {CurrentModuleName.Value ?? "<global>"}" +
                      $"{Environment.NewLine}Matching rule: {result.MatchingRule}" +
                      $"{Environment.NewLine}Candidate paths:" +
                      $"{Environment.NewLine}  1. {candidates.ElementAtOrDefault(0) ?? "<none>"}" +
                      $"{Environment.NewLine}  2. {candidates.ElementAtOrDefault(1) ?? "<none>"}" +
                      $"{Environment.NewLine}  3. {candidates.ElementAtOrDefault(2) ?? "<none>"}" +
                      $"{Environment.NewLine}  4. {candidates.ElementAtOrDefault(3) ?? "<none>"}" +
                      $"{Environment.NewLine}Searched assemblies: {string.Join("; ", result.SearchedAssemblies)}" +
                      $"{Environment.NewLine}Default factory configured: {_factory is not null}" +
                      $"{Environment.NewLine}Scoped factory configured: {GetCurrentFactory() is not null}" +
                      $"{Environment.NewLine}Dependency failure: {ExtractExceptionSummary(innerException)}" +
                      $"{Environment.NewLine}A ViewModel must be registered or the default DI factory must be configured.";

        return new ViewModelResolutionException(
            viewType,
            routeName,
            CurrentModuleName.Value,
            result.CandidatePaths,
            result.SearchedAssemblies,
            result.MatchingRule,
            message,
            innerException);
    }

    private static string ExtractExceptionSummary(Exception? exception)
    {
        if (exception is null)
        {
            return "<none>";
        }

        var messages = new List<string>();
        for (var current = exception; current is not null && messages.Count < 4; current = current.InnerException)
        {
            messages.Add($"{current.GetType().Name}: {current.Message}");
        }

        return string.Join(" -> ", messages);
    }

    private static bool IsInDesignMode => Design.IsDesignMode;

    private static void EnsureCanWrite()
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("ViewModelLocationProvider registrations are read-only.");
        }
    }

    private static void ClearCache()
    {
        TypeCache.Clear();
        StrongTypeCache.Clear();
    }

    private static void CleanupCacheIfNeeded()
    {
        var count = Interlocked.Increment(ref _cacheCleanupCounter);
        if (count > int.MaxValue - 1024)
        {
            Interlocked.Exchange(ref _cacheCleanupCounter, 0);
        }

        if (count % 64 != 0)
        {
            return;
        }

        CleanupCache(force: false);
    }

    private static void CleanupCache(bool force)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in TypeCache)
        {
            if (force ||
                !item.Value.IsAlive(CacheEntryLifetime, now) ||
                TypeCache.Count > MaxCacheEntries)
            {
                TypeCache.TryRemove(item.Key, out _);
                _logger.CacheEvicted(item.Key);
            }
        }

        foreach (var item in StrongTypeCache)
        {
            if ((force || item.Value.IsExpired(now) || StrongTypeCache.Count > MaxCacheEntries) &&
                StrongTypeCache.TryRemove(item.Key, out var removedEntry))
            {
                TypeCache[item.Key] = new ViewModelTypeCacheEntry(removedEntry.ViewModelType);
                _logger.CacheEvicted(item.Key);
            }
        }
    }

    private sealed class ViewModelResolutionResult
    {
        public ViewModelResolutionResult(Type? viewModelType, IReadOnlyList<string> candidatePaths, IReadOnlyList<string> searchedAssemblies, string matchingRule)
        {
            ViewModelType = viewModelType;
            CandidatePaths = candidatePaths;
            SearchedAssemblies = searchedAssemblies;
            MatchingRule = matchingRule;
        }

        public Type? ViewModelType { get; }

        public IReadOnlyList<string> CandidatePaths { get; }

        public IReadOnlyList<string> SearchedAssemblies { get; }

        public string MatchingRule { get; }
    }

    private sealed class ViewModelTypeCacheEntry
    {
        private readonly WeakReference<Type> _type;
        private DateTimeOffset _lastAccess;

        public ViewModelTypeCacheEntry(Type type)
        {
            _type = new WeakReference<Type>(type);
            _lastAccess = DateTimeOffset.UtcNow;
        }

        public bool TryGet(out Type type, TimeSpan lifetime)
        {
            if (_type.TryGetTarget(out type!) && DateTimeOffset.UtcNow - _lastAccess <= lifetime)
            {
                _lastAccess = DateTimeOffset.UtcNow;
                return true;
            }

            return false;
        }

        public bool IsAlive(TimeSpan lifetime, DateTimeOffset now)
        {
            return _type.TryGetTarget(out _) && now - _lastAccess <= lifetime;
        }
    }

    private sealed class StrongViewModelTypeCacheEntry
    {
        private readonly DateTimeOffset? _expiresAt;

        public StrongViewModelTypeCacheEntry(Type viewModelType, TimeSpan? lifetime)
        {
            ViewModelType = viewModelType;
            _expiresAt = lifetime is null ? null : DateTimeOffset.UtcNow.Add(lifetime.Value);
        }

        public Type ViewModelType { get; }

        public bool IsExpired(DateTimeOffset now)
        {
            return _expiresAt is not null && now >= _expiresAt.Value;
        }
    }

    private sealed class ScopeFactoryRegistration
    {
        public ScopeFactoryRegistration(Func<object?, Type, object> factory, Guid? parentScopeId, string? moduleName)
        {
            Factory = factory;
            ParentScopeId = parentScopeId;
            ModuleName = moduleName;
        }

        public Func<object?, Type, object> Factory { get; }

        public Guid? ParentScopeId { get; }

        public string? ModuleName { get; }
    }

    private sealed class ScopeUsageCounter
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Increment()
        {
            Interlocked.Increment(ref _count);
        }

        public int Decrement()
        {
            return Interlocked.Decrement(ref _count);
        }
    }

    private sealed class WildcardRegistration
    {
        public WildcardRegistration(string fullKey, string pattern, Type viewModelType)
        {
            FullKey = fullKey;
            Pattern = pattern;
            ViewModelType = viewModelType;
        }

        public string FullKey { get; }

        public string Pattern { get; }

        public Type ViewModelType { get; }
    }

    private sealed class DisposableAction : IDisposable
    {
        private readonly Action _dispose;
        private int _disposed;

        public DisposableAction(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _dispose();
            }
        }
    }
}