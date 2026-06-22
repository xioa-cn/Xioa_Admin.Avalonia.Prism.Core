using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Prism.Core.Mvvm;
using Prism.Ioc;
using Prism.Mvvm;

namespace Prism.Navigation.Regions;

public sealed class RegionManager : IRegionManager
{
    private static readonly TimeSpan DefaultNavigationTimeout = TimeSpan.FromSeconds(30);
    private const int DefaultMaxCompletedNavigationOnceKeyCount = 1024;
    private static readonly Dictionary<string, NavigationRoutePattern> RoutePatternCache = new(StringComparer.Ordinal);
    private static readonly object RoutePatternCacheSyncRoot = new();

    public static readonly AttachedProperty<string?> RegionNameProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("RegionName", typeof(RegionManager));

    public static readonly AttachedProperty<object?> RegionContextProperty =
        AvaloniaProperty.RegisterAttached<Control, object?>("RegionContext", typeof(RegionManager));

    public static readonly AttachedProperty<IRegionManager?> RegionManagerProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, IRegionManager?>("RegionManager", typeof(RegionManager));

    public static readonly AttachedProperty<bool> CreateRegionManagerScopeProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("CreateRegionManagerScope", typeof(RegionManager));

    public static readonly AttachedProperty<bool> IsActiveViewProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsActiveView", typeof(RegionManager));

    public static readonly AttachedProperty<object?> ActiveViewProperty =
        AvaloniaProperty.RegisterAttached<Control, object?>("ActiveView", typeof(RegionManager));

    public static readonly AttachedProperty<int> ActiveIndexProperty =
        AvaloniaProperty.RegisterAttached<Control, int>("ActiveIndex", typeof(RegionManager), -1);

    private readonly IContainerProvider _container;
    private readonly Dictionary<string, Control> _regionTargets = new(StringComparer.Ordinal);
    private readonly Dictionary<Control, string> _regionNamesByTarget = new();
    private readonly Dictionary<Control, EventHandler<VisualTreeAttachmentEventArgs>> _detachHandlers = new();
    private readonly Dictionary<string, IRegionAdapter> _regionAdapters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Func<object>>> _registeredViews = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object> _activeViews = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _navigationAliases = new(StringComparer.Ordinal);
    private readonly List<NavigationRouteRegistration> _navigationRoutes = new();
    private readonly Dictionary<string, RegionNavigationLock> _navigationLocks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _navigationOnceAliases = new(StringComparer.Ordinal);
    private readonly HashSet<string> _completedNavigationOnceKeys = new(StringComparer.Ordinal);
    private readonly HashSet<string> _detachedRegionNames = new(StringComparer.Ordinal);
    private readonly HashSet<string> _knownRegionNames = new(StringComparer.Ordinal);
    private readonly Queue<string> _completedNavigationOnceQueue = new();
    private readonly List<INavigationInterceptor> _navigationInterceptors = new();
    private readonly HashSet<object> _movingViews = new(ReferenceEqualityComparer.Instance);
    private readonly ConditionalWeakTable<object, AwareTargetCacheEntry> _awareTargetCache = new();
    private readonly RegionAdapterMappings _adapterMappings;
    private readonly IRegionBehaviorFactory _behaviorFactory;
    private readonly IRegionViewFactory _viewFactory;
    private readonly object _syncRoot = new();
    private int _maxCompletedNavigationOnceKeyCount = DefaultMaxCompletedNavigationOnceKeyCount;

    public RegionManager(IContainerProvider container)
        : this(
            container,
            container.IsRegistered(typeof(RegionAdapterMappings))
                ? container.Resolve<RegionAdapterMappings>()
                : new RegionAdapterMappings(),
            container.IsRegistered(typeof(IRegionBehaviorFactory))
                ? container.Resolve<IRegionBehaviorFactory>()
                : new RegionBehaviorFactory())
    {
    }

    private RegionManager(IContainerProvider container, RegionAdapterMappings adapterMappings,
        IRegionBehaviorFactory behaviorFactory)
    {
        _container = container;
        _adapterMappings = adapterMappings;
        _behaviorFactory = behaviorFactory;
        _viewFactory = container.IsRegistered(typeof(IRegionViewFactory))
            ? container.Resolve<IRegionViewFactory>()
            : new ContainerRegionViewFactory(container);
    }

    public IRegionCollection Regions { get; } = new RegionCollection();

    public event EventHandler<RegionNavigationFailedEventArgs>? NavigationFailed;

    public event EventHandler<RegionChangedEventArgs>? RegionRegistered;

    public event EventHandler<RegionChangedEventArgs>? RegionRemoved;

    public TimeSpan NavigationTimeout { get; set; } = DefaultNavigationTimeout;

    public int MaxCompletedNavigationOnceKeyCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _maxCompletedNavigationOnceKeyCount;
            }
        }
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "Navigation once cache capacity must be greater than zero.");
            }

            lock (_syncRoot)
            {
                _maxCompletedNavigationOnceKeyCount = value;
                TrimCompletedNavigationOnceKeys();
            }
        }
    }

    static RegionManager()
    {
        RegionNameProperty.Changed.AddClassHandler<Control>((control, args) =>
        {
            if (args.NewValue is not string { Length: > 0 } regionName)
            {
                return;
            }

            if (FindRegionManager(control) is RegionManager regionManager)
            {
                regionManager.RegisterRegion(control, regionName);
                return;
            }

            control.AttachedToVisualTree += RegisterRegionWhenAttached;
        });

        RegionContextProperty.Changed.AddClassHandler<Control>((control, args) =>
        {
            if (GetRegionName(control) is { Length: > 0 } regionName &&
                FindRegionManager(control) is { } regionManager &&
                regionManager.Regions.ContainsRegionWithName(regionName))
            {
                regionManager.Regions[regionName].Context = args.NewValue;
            }
        });

        RegionManagerProperty.Changed.AddClassHandler<Control>((control, args) =>
        {
            if (args.NewValue is RegionManager regionManager)
            {
                regionManager.RegisterRegions(control);
            }
        });
    }

    public IRegionManager CreateRegionManager()
    {
        return new RegionManager(_container.CreateScope(), _adapterMappings, _behaviorFactory);
    }

    public IRegionManager AddToRegion(string regionName, object view)
    {
        var regionTarget = GetRegionTarget(regionName);
        _ = AddRegionView(regionName, regionTarget, view, activate: false);
        return this;
    }

    public IRegionManager AddToRegion(string regionName, string viewName)
    {
        return AddToRegion(regionName, _viewFactory.CreateView(viewName));
    }

    public IRegionManager RegisterViewWithRegion(string regionName, string viewName)
    {
        return RegisterViewWithRegion(regionName, () => _viewFactory.CreateView(viewName));
    }

    public IRegionManager RegisterViewWithRegion(string regionName, Type viewType)
    {
        return RegisterViewWithRegion(regionName, () => _viewFactory.CreateView(viewType));
    }

    public IRegionManager RegisterViewWithRegion(string regionName, Func<IContainerProvider, object> getContentDelegate)
    {
        return RegisterViewWithRegion(regionName, () => getContentDelegate(_container));
    }

    public IRegionManager RegisterViewWithRegion(string regionName, Func<object> getContentDelegate)
    {
        if (string.IsNullOrWhiteSpace(regionName))
        {
            throw new ArgumentException("Region name cannot be empty.", nameof(regionName));
        }

        ArgumentNullException.ThrowIfNull(getContentDelegate);

        lock (_syncRoot)
        {
            if (!_registeredViews.TryGetValue(regionName, out var views))
            {
                views = new List<Func<object>>();
                _registeredViews[regionName] = views;
            }

            views.Add(getContentDelegate);
        }

        return this;
    }

    public IRegionManager AddNavigationInterceptor(INavigationInterceptor interceptor)
    {
        lock (_syncRoot)
        {
            _navigationInterceptors.Add(interceptor);
        }

        return this;
    }

    public IRegionManager RegisterNavigationAlias(string alias, string target)
    {
        lock (_syncRoot)
        {
            _navigationAliases[alias] = target;
        }

        return this;
    }

    public IRegionManager RegisterNavigationRoute(string routeTemplate, string target)
    {
        return RegisterNavigationRoute(routeTemplate, target, _ => true);
    }

    public IRegionManager RegisterNavigationRoute(string routeTemplate, string target,
        Func<INavigationParameters, bool> constraint)
    {
        if (string.IsNullOrWhiteSpace(routeTemplate))
        {
            throw new ArgumentException("Route template cannot be empty.", nameof(routeTemplate));
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("Route target cannot be empty.", nameof(target));
        }

        var route = CreateRouteRegistration(routeTemplate, target, constraint);
        lock (_syncRoot)
        {
            _navigationRoutes.Add(route);
        }

        return this;
    }

    public Task<NavigationResult> RequestNavigateOnceAsync(string regionName, string source,
        INavigationParameters? navigationParameters = null)
    {
        var alias = GetNavigationAliasRoute(source);
        var resolvedSource = ResolveNavigationAlias(source);
        var key = GetNavigationOnceKey(regionName, resolvedSource);
        lock (_syncRoot)
        {
            if (_completedNavigationOnceKeys.Contains(key))
            {
                return Task.FromResult(new NavigationResult(false));
            }
        }

        if (alias is not null)
        {
            lock (_syncRoot)
            {
                _navigationOnceAliases[key] = alias;
            }
        }

        return NavigateAsync(regionName, resolvedSource, navigationParameters ?? new NavigationParameters());
    }

    [Obsolete(
        "Use RequestNavigateAsync instead. Synchronous region navigation can block UI threads and cannot reliably return the newly navigated view.")]
    public object? RequestNavigate(string regionName, string source)
    {
        if (SynchronizationContext.Current is not null)
        {
            throw new InvalidOperationException(
                "Synchronous region navigation is not supported on a UI synchronization context. Use RequestNavigateAsync.");
        }

        var result = Task.Run(() => RequestNavigateAsync(regionName, source)).GetAwaiter().GetResult();
        if (result.Exception is not null)
        {
            throw result.Exception;
        }

        lock (_syncRoot)
        {
            return _activeViews.TryGetValue(regionName, out var activeView) ? activeView : null;
        }
    }

    [Obsolete("Use RequestNavigateAsync instead.")]
    public void RequestNavigate(string regionName, string source, Action<NavigationResult>? navigationCallback)
    {
        RequestNavigate(regionName, source, navigationCallback, null);
    }

    [Obsolete("Use RequestNavigateAsync instead.")]
    public void RequestNavigate(string regionName, string source, Action<NavigationResult>? navigationCallback,
        INavigationParameters? navigationParameters)
    {
        _ = RequestNavigateAsync(regionName, source, navigationParameters)
            .ContinueWith(task =>
            {
                var result = task.Status == TaskStatus.RanToCompletion
                    ? task.Result
                    : new NavigationResult(task.Exception?.GetBaseException() ??
                                           new InvalidOperationException("Navigation failed."));
                navigationCallback?.Invoke(result);
            }, TaskScheduler.Default);
    }

    [Obsolete("Use RequestNavigateAsync instead.")]
    public void RequestNavigate(string regionName, Uri source, Action<NavigationResult>? navigationCallback)
    {
        RequestNavigate(regionName, source, navigationCallback, null);
    }

    [Obsolete("Use RequestNavigateAsync instead.")]
    public void RequestNavigate(string regionName, Uri source, Action<NavigationResult>? navigationCallback,
        INavigationParameters? navigationParameters)
    {
        RequestNavigate(regionName, source.OriginalString, navigationCallback, navigationParameters);
    }

    public Task<NavigationResult> RequestNavigateAsync(string regionName, string source,
        INavigationParameters? navigationParameters = null)
    {
        return NavigateAsync(regionName, ResolveNavigationAlias(source),
            navigationParameters ?? new NavigationParameters());
    }

    public Task<NavigationResult> RequestNavigateAsync(string regionName, Uri source,
        INavigationParameters? navigationParameters = null)
    {
        return RequestNavigateAsync(regionName, source.OriginalString, navigationParameters);
    }

    internal Task<NavigationResult> NavigateFromJournalAsync(string regionName, string source,
        INavigationParameters navigationParameters)
    {
        return NavigateAsync(regionName, ResolveNavigationAlias(source), navigationParameters, recordJournal: false);
    }

    internal void RegisterRegion(Control control, string regionName)
    {
        if (!_adapterMappings.TryGetMapping(control.GetType(), _container, out var adapter))
        {
            throw new InvalidOperationException($"No region adapter is registered for {control.GetType().FullName}.");
        }

        lock (_syncRoot)
        {
            _regionTargets[regionName] = control;
            _regionNamesByTarget[control] = regionName;
            _regionAdapters[regionName] = adapter;
            _detachedRegionNames.Remove(regionName);
            _knownRegionNames.Add(regionName);
            AttachDetachHandler(control);
        }

        if (!Regions.ContainsRegionWithName(regionName))
        {
            var region = adapter.Initialize(control, regionName, this);
            region.Context = GetRegionContext(control);
            Regions.Add(region);
            AttachBehaviors(region);
            RegionRegistered?.Invoke(this, new RegionChangedEventArgs(region, control));
        }
    }

    internal void UpdateRegisteredRegions()
    {
        var regionTargets = new List<KeyValuePair<string, Control>>();
        lock (_syncRoot)
        {
            foreach (var item in _regionTargets)
            {
                regionTargets.Add(item);
            }
        }

        foreach (var (regionName, control) in regionTargets)
        {
            GetOrCreateRegion(regionName, control, GetAdapter(regionName, control));
        }
    }

    private Task<NavigationResult> NavigateAsync(string regionName, string source,
        INavigationParameters navigationParameters)
    {
        return NavigateAsync(regionName, source, navigationParameters, recordJournal: true);
    }

    private async Task<NavigationResult> NavigateAsync(string regionName, string source,
        INavigationParameters navigationParameters, bool recordJournal)
    {
        source = ResolveNavigationRoute(source, navigationParameters);
        var navigationLock = RentNavigationLock(regionName);
        if (navigationLock is null || !await navigationLock.WaitAsync(NavigationTimeout).ConfigureAwait(false))
        {
            navigationLock?.ReleaseRent(releaseSemaphore: false);
            return new NavigationResult(false);
        }

        try
        {
            var targetCandidates = GetNavigationTargetCandidates(source, navigationParameters);
            var navigationContext = new NavigationContext(regionName, new Uri(source, UriKind.RelativeOrAbsolute),
                navigationParameters);
            var (regionTarget, region, oldContent) = await RunOnUiThreadAsync(() =>
            {
                var target = GetRegionTarget(regionName);
                return (target, GetOrCreateRegion(regionName, target, GetAdapter(regionName, target)),
                    GetActiveView(regionName));
            }).ConfigureAwait(false);
            var logger = GetNavigationLogger();

            logger.NavigationStarting(navigationContext);

            if (!await CanNavigateAsync(navigationContext).ConfigureAwait(false))
            {
                logger.NavigationCanceled(navigationContext);
                return new NavigationResult(false);
            }

            if (!await RunOnUiThreadAsync(() => ConfirmNavigationAsync(oldContent, navigationContext))
                    .ConfigureAwait(false))
            {
                logger.NavigationCanceled(navigationContext);
                return new NavigationResult(false);
            }

            var content =
                await RunOnUiThreadAsync(() => SelectNavigationTarget(regionName, targetCandidates, navigationContext))
                    .ConfigureAwait(false);

            if (ReferenceEquals(oldContent, content))
            {
                await RunOnUiThreadAsync(() => WireNavigationViewModel(content, source))
                    .ConfigureAwait(false);
                await RunOnUiThreadAsync(() => NotifyRepeatedNavigationAsync(content, navigationContext))
                    .ConfigureAwait(false);
                if (recordJournal && region is Region repeatedNavigationRegion)
                {
                    repeatedNavigationRegion.InternalNavigationJournal.Record(navigationContext);
                }

                logger.NavigationSucceeded(navigationContext, content);
                CompleteNavigationOnce(regionName, source);
                return new NavigationResult(true);
            }

            if (oldContent is not null)
            {
                await RunOnUiThreadAsync(() =>
                        NotifyNavigatedFromAsync(regionName, regionTarget, region, oldContent, content,
                            navigationContext))
                    .ConfigureAwait(false);
            }

            await RunOnUiThreadAsync(() => WireNavigationViewModel(content, source))
                .ConfigureAwait(false);
            await RunOnUiThreadAsync(() => NotifyBeforeViewLoad(content, navigationContext))
                .ConfigureAwait(false);
            await RunOnUiThreadAsync(() => InitializeViewAsync(content, navigationContext)).ConfigureAwait(false);
            await RunOnUiThreadAsync(() =>
                    AddRegionView(regionName, regionTarget, content, activate: true, oldContent, navigationContext))
                .ConfigureAwait(false);
            await RunOnUiThreadAsync(() =>
                    RunNavigationEnterAnimationAsync(regionName, regionTarget, region, oldContent, content,
                        navigationContext))
                .ConfigureAwait(false);
            await RunOnUiThreadAsync(() => NotifyNavigatedTo(content, navigationContext))
                .ConfigureAwait(false);
            if (recordJournal && region is Region concreteRegion)
            {
                concreteRegion.InternalNavigationJournal.Record(navigationContext);
            }

            logger.NavigationSucceeded(navigationContext, content);
            CompleteNavigationOnce(regionName, source);
            return new NavigationResult(true);
        }
        catch (Exception ex)
        {
            var context = new NavigationContext(regionName, new Uri(source, UriKind.RelativeOrAbsolute),
                navigationParameters);
            GetNavigationLogger().NavigationFailed(context, ex);
            OnNavigationFailed(context, null, ex);
            return new NavigationResult(ex);
        }
        finally
        {
            ReleaseNavigationLock(navigationLock);
        }
    }

    private RegionNavigationLock? RentNavigationLock(string regionName)
    {
        lock (_syncRoot)
        {
            if (!_navigationLocks.TryGetValue(regionName, out var navigationLock))
            {
                navigationLock =
                    new RegionNavigationLock(disposedLock => RemoveNavigationLock(regionName, disposedLock));
                _navigationLocks[regionName] = navigationLock;
            }

            return navigationLock.TryRent() ? navigationLock : null;
        }
    }

    private static void ReleaseNavigationLock(RegionNavigationLock navigationLock)
    {
        navigationLock.ReleaseRent(releaseSemaphore: true);
    }

    private void RemoveNavigationLock(string regionName, RegionNavigationLock navigationLock)
    {
        lock (_syncRoot)
        {
            if (_navigationLocks.TryGetValue(regionName, out var currentLock) &&
                ReferenceEquals(currentLock, navigationLock))
            {
                _navigationLocks.Remove(regionName);
            }
        }
    }

    private static Task RunOnUiThreadAsync(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                action();
                completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        return completion.Task;
    }

    private void WireNavigationViewModel(object view, string source)
    {
        if (view is not StyledElement styledElement || styledElement.DataContext is not null)
        {
            return;
        }

        var vmType = ViewModelLocationProvider.ResolveViewModelTypeForNavigation(view.GetType(), routeName: source);
        if (vmType is not null)
        {
            styledElement.DataContext = _container.Resolve(vmType);
        }
    }

    private static Task<T> RunOnUiThreadAsync<T>(Func<T> action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return Task.FromResult(action());
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                completion.TrySetResult(action());
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        return completion.Task;
    }

    private static Task<T> RunOnUiThreadAsync<T>(Func<Task<T>> action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return action();
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                completion.TrySetResult(await action());
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        return completion.Task;
    }

    private static Task RunOnUiThreadAsync(Func<Task> action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return action();
        }

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await action();
                completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        return completion.Task;
    }

    private string ResolveNavigationAlias(string source)
    {
        var queryIndex = source.IndexOf('?', StringComparison.Ordinal);
        var route = queryIndex >= 0 ? source[..queryIndex] : source;
        var query = queryIndex >= 0 ? source[queryIndex..] : string.Empty;

        lock (_syncRoot)
        {
            if (_navigationAliases.TryGetValue(route, out var exactTarget))
            {
                return exactTarget + query;
            }

            foreach (var (alias, target) in _navigationAliases)
            {
                if (IsWildcardAliasMatch(alias, route))
                {
                    return target + query;
                }
            }

            return source;
        }
    }

    private string? GetNavigationAliasRoute(string source)
    {
        var queryIndex = source.IndexOf('?', StringComparison.Ordinal);
        var route = queryIndex >= 0 ? source[..queryIndex] : source;
        lock (_syncRoot)
        {
            if (_navigationAliases.ContainsKey(route))
            {
                return route;
            }

            return _navigationAliases.Keys.FirstOrDefault(alias => IsWildcardAliasMatch(alias, route));
        }
    }

    private static bool IsWildcardAliasMatch(string alias, string route)
    {
        var wildcardIndex = alias.IndexOf('*', StringComparison.Ordinal);
        if (wildcardIndex < 0)
        {
            return false;
        }

        var prefix = alias[..wildcardIndex];
        var suffix = alias[(wildcardIndex + 1)..];
        return route.StartsWith(prefix, StringComparison.Ordinal) &&
               route.EndsWith(suffix, StringComparison.Ordinal) &&
               route.Length >= prefix.Length + suffix.Length;
    }

    private string ResolveNavigationRoute(string source, INavigationParameters parameters)
    {
        List<NavigationRouteRegistration> routes;
        lock (_syncRoot)
        {
            if (_navigationRoutes.Count == 0)
            {
                return source;
            }

            routes = new List<NavigationRouteRegistration>(_navigationRoutes);
        }

        var queryIndex = source.IndexOf('?', StringComparison.Ordinal);
        var route = "/" + (queryIndex >= 0 ? source[..queryIndex] : source).Trim('/');
        var query = queryIndex >= 0 ? source[queryIndex..] : string.Empty;

        foreach (var registration in routes)
        {
            var match = registration.Regex.Match(route);
            if (!match.Success)
            {
                continue;
            }

            var routeParameters = CloneNavigationParameters(parameters);
            foreach (var defaultValue in registration.DefaultValues)
            {
                if (!routeParameters.ContainsKey(defaultValue.Key))
                {
                    routeParameters.Add(defaultValue.Key, defaultValue.Value, NavigationParameterScope.Route);
                }
            }

            foreach (var parameterName in registration.ParameterNames)
            {
                var group = match.Groups[parameterName];
                if (group.Success && !routeParameters.ContainsKey(parameterName))
                {
                    routeParameters.Add(parameterName, Uri.UnescapeDataString(group.Value),
                        NavigationParameterScope.Route);
                }
            }

            if (registration.Constraint?.Invoke(routeParameters) == false)
            {
                continue;
            }

            foreach (var parameter in routeParameters)
            {
                if (!parameters.ContainsKey(parameter.Key))
                {
                    var scope = routeParameters.ContainsKey(parameter.Key, NavigationParameterScope.Route)
                        ? NavigationParameterScope.Route
                        : NavigationParameterScope.Parameters;
                    parameters.Add(parameter.Key, parameter.Value, scope);
                }
            }

            return registration.Target + query;
        }

        return source;
    }

    private static NavigationRouteRegistration CreateRouteRegistration(string routeTemplate, string target,
        Func<INavigationParameters, bool>? constraint)
    {
        var pattern = GetOrCreateRoutePattern(routeTemplate);
        return new NavigationRouteRegistration(
            routeTemplate,
            target,
            pattern.Regex,
            pattern.ParameterNames,
            pattern.DefaultValues,
            constraint);
    }

    private static NavigationRoutePattern GetOrCreateRoutePattern(string routeTemplate)
    {
        lock (RoutePatternCacheSyncRoot)
        {
            if (RoutePatternCache.TryGetValue(routeTemplate, out var cachedPattern))
            {
                return cachedPattern;
            }
        }

        var compiledPattern = CompileRoutePattern(routeTemplate);
        lock (RoutePatternCacheSyncRoot)
        {
            if (RoutePatternCache.TryGetValue(routeTemplate, out var cachedPattern))
            {
                return cachedPattern;
            }

            RoutePatternCache[routeTemplate] = compiledPattern;
            return compiledPattern;
        }
    }

    private static NavigationRoutePattern CompileRoutePattern(string routeTemplate)
    {
        var parameterNames = new List<string>();
        var defaultValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        var template = routeTemplate.Trim('/');
        var segments = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pattern = "^";

        foreach (var segment in segments)
        {
            if (segment.StartsWith("{", StringComparison.Ordinal) && segment.EndsWith("}", StringComparison.Ordinal))
            {
                var body = segment[1..^1];
                var optional = body.EndsWith("?", StringComparison.Ordinal);
                if (optional)
                {
                    body = body[..^1];
                }

                var defaultValue = default(string);
                var defaultIndex = body.IndexOf('=', StringComparison.Ordinal);
                if (defaultIndex >= 0)
                {
                    defaultValue = Uri.UnescapeDataString(body[(defaultIndex + 1)..]);
                    body = body[..defaultIndex];
                    optional = true;
                }

                var parts = body.Split(':', 2);
                var name = parts[0];
                var constraintName = parts.Length > 1 ? parts[1] : null;
                parameterNames.Add(name);
                if (defaultValue is not null)
                {
                    defaultValues[name] = defaultValue;
                }

                var valuePattern = constraintName switch
                {
                    "int" => "\\d+",
                    "guid" => "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
                    "alpha" => "[A-Za-z]+",
                    _ => "[^/]+"
                };

                pattern += optional
                    ? $"(?:/(?<{name}>{valuePattern}))?"
                    : $"/(?<{name}>{valuePattern})";
                continue;
            }

            pattern += "/" + Regex.Escape(segment);
        }

        pattern += "$";
        return new NavigationRoutePattern(
            new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            parameterNames,
            defaultValues);
    }

    private void CompleteNavigationOnce(string regionName, string source)
    {
        var key = GetNavigationOnceKey(regionName, source);
        lock (_syncRoot)
        {
            if (!_navigationOnceAliases.Remove(key, out var alias))
            {
                AddCompletedNavigationOnceKey(key);
                return;
            }

            _navigationAliases.Remove(alias);
            AddCompletedNavigationOnceKey(key);
        }
    }

    private void AddCompletedNavigationOnceKey(string key)
    {
        if (!_completedNavigationOnceKeys.Add(key))
        {
            return;
        }

        _completedNavigationOnceQueue.Enqueue(key);
        TrimCompletedNavigationOnceKeys();
    }

    private void TrimCompletedNavigationOnceKeys()
    {
        while (_completedNavigationOnceQueue.Count > _maxCompletedNavigationOnceKeyCount)
        {
            _completedNavigationOnceKeys.Remove(_completedNavigationOnceQueue.Dequeue());
        }
    }

    private static string GetNavigationOnceKey(string regionName, string source)
    {
        return regionName + '\u001f' + source;
    }

    private static IReadOnlyList<string> GetNavigationTargetCandidates(string source, INavigationParameters parameters)
    {
        var uri = new Uri(source, UriKind.RelativeOrAbsolute);
        var target = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
        var queryIndex = target.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
        {
            MergeQueryParameters(target[(queryIndex + 1)..], parameters);
            target = target[..queryIndex];
        }

        target = target.Trim('/');
        var slashName = target.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        var dotName = slashName?.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

        var candidates = new List<string>(4);
        AddCandidate(candidates, source);
        AddCandidate(candidates, target);
        AddCandidate(candidates, slashName);
        AddCandidate(candidates, dotName);
        return candidates;
    }

    private static void AddCandidate(List<string> candidates, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) || candidates.Contains(candidate, StringComparer.Ordinal))
        {
            return;
        }

        candidates.Add(candidate);
    }

    private static void MergeQueryParameters(string query, INavigationParameters parameters)
    {
        foreach (var parameter in new NavigationParameters(query))
        {
            if (!parameters.ContainsKey(parameter.Key))
            {
                parameters.Add(parameter.Key, parameter.Value, NavigationParameterScope.Query);
            }
        }
    }

    private static NavigationParameters CloneNavigationParameters(INavigationParameters source)
    {
        if (source.Clone() is NavigationParameters clonedParameters)
        {
            return clonedParameters;
        }

        var clone = new NavigationParameters();
        foreach (var parameter in source)
        {
            clone.Add(parameter.Key, parameter.Value);
        }

        return clone;
    }

    private object SelectNavigationTarget(string regionName, IReadOnlyList<string> targetCandidates,
        NavigationContext navigationContext)
    {
        if (Regions.ContainsRegionWithName(regionName))
        {
            foreach (var view in Regions[regionName].Views)
            {
                if (targetCandidates.Any(candidate => IsViewMatch(view, candidate)) &&
                    IsNavigationTarget(view, navigationContext))
                {
                    return view;
                }
            }
        }

        foreach (var targetName in targetCandidates)
        {
            if (!_container.IsRegistered(typeof(object), targetName))
            {
                continue;
            }

            return _viewFactory.CreateView(targetName);
        }

        throw new InvalidOperationException(
            $"Navigation target '{string.Join("', '", targetCandidates)}' is not registered.");
    }

    private bool IsNavigationTarget(object view, NavigationContext navigationContext)
    {
        var targets = GetAwareTargets(view);
        foreach (var target in targets)
        {
            if (target is not INavigationAware awareTarget)
            {
                continue;
            }

            try
            {
                if (!awareTarget.IsNavigationTarget(navigationContext))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnNavigationFailed(navigationContext, awareTarget, ex);
                return false;
            }
        }

        return true;
    }

    private static bool IsViewMatch(object view, string targetName)
    {
        var viewType = view.GetType();
        return string.Equals(viewType.Name, targetName, StringComparison.Ordinal) ||
               string.Equals(viewType.FullName, targetName, StringComparison.Ordinal) ||
               string.Equals(viewType.AssemblyQualifiedName, targetName, StringComparison.Ordinal);
    }

    private void EnsureRegionManagerForView(Control regionTarget, Control view)
    {
        if (GetRegionManager(view) is not null)
        {
            return;
        }

        var parentManager = FindRegionManager(regionTarget) ?? this;
        var viewManager = GetCreateRegionManagerScope(view)
            ? parentManager.CreateRegionManager()
            : parentManager;

        SetRegionManager(view, viewManager);
    }

    private async Task AddRegionView(
        string regionName,
        Control regionTarget,
        object content,
        bool activate,
        object? oldContent = null,
        NavigationContext? navigationContext = null)
    {
        if (content is Control control)
        {
            using (ViewModelLocationProvider.UseScopedViewModelFactory((view, type) => _container.Resolve(type)))
            {
                control.DataContext ??= ViewModelLocationProvider.AutoWireViewModelChanged(control);
            }

            EnsureRegionManagerForView(regionTarget, control);
        }

        MoveExistingViewToRegion(regionName, content);

        var adapter = GetAdapter(regionName, regionTarget);
        var region = GetOrCreateRegion(regionName, regionTarget, adapter);
        if (!region.Views.Contains(content))
        {
            region.Add(content);
            if (!activate || regionTarget is not ContentControl)
            {
                adapter.AddView(regionTarget, content);
            }

            RefreshActiveRegionState(regionTarget, region);
        }

        if (activate)
        {
            await ActivateRegionViewAsync(regionName, regionTarget, region, adapter, content, oldContent,
                    navigationContext)
                .ConfigureAwait(false);
        }
    }

    private async Task ActivateRegionViewAsync(
        string regionName,
        Control regionTarget,
        IRegion region,
        IRegionAdapter adapter,
        object content,
        object? oldContent,
        NavigationContext? navigationContext)
    {
        var activeViews = new List<object>();
        region.ActiveViews.CopyTo(activeViews);
        foreach (var activeView in activeViews)
        {
            if (ReferenceEquals(activeView, content))
            {
                continue;
            }

            region.Deactivate(activeView);
            SetViewActiveMarker(activeView, false);
            adapter.DeactivateView(regionTarget, activeView);

            if (!ShouldKeepAlive(activeView))
            {
                await NotifyVmUnloadedAsync(activeView).ConfigureAwait(false);
                region.Remove(activeView);
                adapter.RemoveView(regionTarget, activeView);
            }
        }

        region.Activate(content);
        SetViewActiveMarker(content, true);
        SetActiveRegionState(regionTarget, region, content);
        PrepareContentReplacementAnimation(regionName, regionTarget, region, oldContent, content, navigationContext);
        adapter.ActivateView(regionTarget, content);
        RegisterNestedRegions(content);
        await NotifyVmLoadedAsync(content).ConfigureAwait(false);
        if (navigationContext is null)
        {
            EnsureViewVisible(content);
        }

        lock (_syncRoot)
        {
            _activeViews[regionName] = content;
        }
    }

    private void RegisterNestedRegions(object content)
    {
        if (content is not Control control)
        {
            return;
        }

        var regionManager = FindRegionManager(control) as RegionManager ?? this;
        regionManager.RestoreDetachedRegions(control);
        Dispatcher.UIThread.Post(
            () => regionManager.RestoreDetachedRegions(control),
            DispatcherPriority.Loaded);
    }

    private void RestoreDetachedRegions(Control root)
    {
        foreach (var control in EnumerateRegionCandidates(root))
        {
            if (GetRegionName(control) is not { Length: > 0 } regionName)
            {
                continue;
            }

            lock (_syncRoot)
            {
                if (!_knownRegionNames.Contains(regionName) || _regionTargets.ContainsKey(regionName))
                {
                    continue;
                }
            }

            RegisterRegion(control, regionName);
        }
    }

    private static async Task NotifyVmLoadedAsync(object view)
    {
        foreach (var target in GetLoadedTargets(view))
        {
            switch (target)
            {
                case IVmLoadedAsync asyncLoaded:
                    await asyncLoaded.LoadAsync().ConfigureAwait(false);
                    break;
                case IVmLoaded loaded:
                    loaded.Load();
                    break;
            }
        }
    }

    private static async Task NotifyVmUnloadedAsync(object view)
    {
        foreach (var target in GetLoadedTargets(view))
        {
            switch (target)
            {
                case IVmLoadedAsync asyncLoaded:
                    await asyncLoaded.UnloadAsync().ConfigureAwait(false);
                    break;
                case IVmLoaded loaded:
                    loaded.Unload();
                    break;
            }
        }
    }

    private static IEnumerable<object> GetLoadedTargets(object view)
    {
        if (view is IVmLoaded or IVmLoadedAsync)
        {
            yield return view;
        }

        if (view is Control { DataContext: { } dataContext } &&
            !ReferenceEquals(dataContext, view) &&
            dataContext is IVmLoaded or IVmLoadedAsync)
        {
            yield return dataContext;
        }
    }

    private void MoveExistingViewToRegion(string targetRegionName, object view)
    {
        var regions = new List<IRegion>();
        Regions.CopyTo(regions);
        lock (_syncRoot)
        {
            _movingViews.Add(view);
        }

        try
        {
            foreach (var region in regions)
            {
                if (string.Equals(region.Name, targetRegionName, StringComparison.Ordinal) ||
                    !region.Views.Contains(view))
                {
                    continue;
                }

                Control? oldTarget;
                IRegionAdapter? oldAdapter;
                lock (_syncRoot)
                {
                    _regionTargets.TryGetValue(region.Name, out oldTarget);
                    _regionAdapters.TryGetValue(region.Name, out oldAdapter);
                    if (_activeViews.TryGetValue(region.Name, out var activeView) && ReferenceEquals(activeView, view))
                    {
                        _activeViews.Remove(region.Name);
                    }
                }

                region.Remove(view);
                SetViewActiveMarker(view, false);
                if (oldTarget is not null && oldAdapter is not null)
                {
                    oldAdapter.RemoveView(oldTarget, view);
                    RefreshActiveRegionState(oldTarget, region);
                }
            }
        }
        finally
        {
            lock (_syncRoot)
            {
                _movingViews.Remove(view);
            }
        }
    }

    internal bool IsMovingView(object view)
    {
        lock (_syncRoot)
        {
            return _movingViews.Contains(view);
        }
    }

    private IRegion GetOrCreateRegion(string regionName, Control regionTarget, IRegionAdapter adapter)
    {
        if (Regions.ContainsRegionWithName(regionName))
        {
            return Regions[regionName];
        }

        var region = adapter.Initialize(regionTarget, regionName, this);
        region.Context = GetRegionContext(regionTarget);
        Regions.Add(region);
        AttachBehaviors(region);
        RegionRegistered?.Invoke(this, new RegionChangedEventArgs(region, regionTarget));
        return region;
    }

    private IRegionAdapter GetAdapter(string regionName, Control regionTarget)
    {
        lock (_syncRoot)
        {
            if (_regionAdapters.TryGetValue(regionName, out var adapter))
            {
                return adapter;
            }
        }

        if (!_adapterMappings.TryGetMapping(regionTarget.GetType(), _container, out var mappedAdapter))
        {
            throw new InvalidOperationException(
                $"No region adapter is registered for {regionTarget.GetType().FullName}.");
        }

        lock (_syncRoot)
        {
            _regionAdapters[regionName] = mappedAdapter;
        }

        return mappedAdapter;
    }

    private Control GetRegionTarget(string regionName)
    {
        lock (_syncRoot)
        {
            if (_regionTargets.TryGetValue(regionName, out var regionTarget))
            {
                return regionTarget;
            }
        }

        throw new InvalidOperationException($"Region '{regionName}' does not exist.");
    }

    private object? GetActiveView(string regionName)
    {
        lock (_syncRoot)
        {
            return _activeViews.TryGetValue(regionName, out var activeView) ? activeView : null;
        }
    }

    private void AttachDetachHandler(Control control)
    {
        if (_detachHandlers.ContainsKey(control))
        {
            return;
        }

        EventHandler<VisualTreeAttachmentEventArgs> handler = (_, _) => CleanupRegion(control);
        _detachHandlers[control] = handler;
        control.DetachedFromVisualTree += handler;
    }

    private void CleanupRegion(Control control)
    {
        string? regionName;
        IRegionAdapter? adapter = null;
        RegionNavigationLock? navigationLock = null;

        lock (_syncRoot)
        {
            if (!_regionNamesByTarget.TryGetValue(control, out regionName))
            {
                return;
            }

            if (ShouldKeepRegionRegistrationOnDetach(control))
            {
                return;
            }

            _regionNamesByTarget.Remove(control);
            _regionTargets.Remove(regionName);
            _activeViews.Remove(regionName);
            _regionAdapters.Remove(regionName, out adapter);
            _detachedRegionNames.Add(regionName);
            _navigationLocks.TryGetValue(regionName, out navigationLock);

            if (_detachHandlers.Remove(control, out var handler))
            {
                control.DetachedFromVisualTree -= handler;
            }
        }

        navigationLock?.MarkRemoved();

        if (Regions.ContainsRegionWithName(regionName))
        {
            var region = Regions[regionName];
            var views = new List<object>();
            region.Views.CopyTo(views);
            foreach (var view in views)
            {
                adapter?.RemoveView(control, view);
                region.Remove(view);
            }

            SetActiveRegionState(control, region, null);
            region.RegionManager = null;

            Regions.Remove(regionName);
            RegionRemoved?.Invoke(this, new RegionChangedEventArgs(region, control));
        }
    }

    private bool ShouldKeepRegionRegistrationOnDetach(Control control)
    {
        if (ShouldKeepAlive(control))
        {
            return true;
        }

        foreach (var ancestor in control.GetLogicalAncestors().OfType<object>())
        {
            if (ShouldKeepAlive(ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private void AttachBehaviors(IRegion region)
    {
        foreach (var (_, behaviorType) in _behaviorFactory.GetBehaviors())
        {
            if (!typeof(IRegionBehavior).IsAssignableFrom(behaviorType))
            {
                continue;
            }

            var behavior = (IRegionBehavior)_container.Resolve(behaviorType);
            behavior.Region = region;
            behavior.Attach();
        }
    }

    private async Task<bool> ConfirmNavigationAsync(object? view, NavigationContext navigationContext)
    {
        var awareTargets = view is null
            ? Array.Empty<object>()
            : GetAwareTargets(view);

        foreach (var target in awareTargets.OfType<IConfirmNavigationRequestAsync>())
        {
            var result = await ConfirmOneAsync(target, navigationContext).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
        }

        foreach (var target in awareTargets.OfType<IConfirmNavigationRequest>())
        {
            var result = await ConfirmOneAsync(target, navigationContext).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ConfirmOneAsync(IConfirmNavigationRequestAsync target, NavigationContext navigationContext)
    {
        try
        {
            var confirmTask = target.ConfirmNavigationRequestAsync(navigationContext);
            var timeoutTask = Task.Delay(NavigationTimeout);
            var completedTask = await Task.WhenAny(confirmTask, timeoutTask).ConfigureAwait(false);
            return completedTask == confirmTask && await confirmTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnNavigationFailed(navigationContext, target, ex);
            return false;
        }
    }

    private async Task<bool> CanNavigateAsync(NavigationContext navigationContext)
    {
        var interceptors = new List<INavigationInterceptor>();
        lock (_syncRoot)
        {
            interceptors.AddRange(_navigationInterceptors);
        }

        foreach (var interceptor in interceptors)
        {
            try
            {
                if (!await interceptor.CanNavigateAsync(navigationContext).ConfigureAwait(false))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnNavigationFailed(navigationContext, interceptor, ex);
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ConfirmOneAsync(IConfirmNavigationRequest target, NavigationContext navigationContext)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            target.ConfirmNavigationRequest(navigationContext, canNavigate => completion.TrySetResult(canNavigate));
        }
        catch (Exception ex)
        {
            OnNavigationFailed(navigationContext, target, ex);
            return false;
        }

        var timeoutTask = Task.Delay(NavigationTimeout);
        var completedTask = await Task.WhenAny(completion.Task, timeoutTask).ConfigureAwait(false);
        return completedTask == completion.Task && await completion.Task.ConfigureAwait(false);
    }

    private async Task NotifyNavigatedFromAsync(
        string regionName,
        Control regionTarget,
        IRegion region,
        object view,
        object? toView,
        NavigationContext navigationContext)
    {
        await RunNavigationExitAnimationAsync(regionName, regionTarget, region, view, toView, navigationContext);

        foreach (var target in GetAwareTargets(view).OfType<INavigationAware>())
        {
            try
            {
                target.OnNavigatedFrom(navigationContext);
            }
            catch (Exception ex)
            {
                OnNavigationFailed(navigationContext, target, ex);
            }
        }
    }

    private void NotifyBeforeViewLoad(object view, NavigationContext navigationContext)
    {
        foreach (var target in GetAwareTargets(view).OfType<IRegionViewPreLoad>())
        {
            try
            {
                target.OnBeforeViewLoad(navigationContext);
            }
            catch (Exception ex)
            {
                OnNavigationFailed(navigationContext, target, ex);
            }
        }
    }

    private void NotifyNavigatedTo(object view, NavigationContext navigationContext)
    {
        foreach (var target in GetAwareTargets(view).OfType<IInitialize>())
        {
            try
            {
                target.Initialize(navigationContext.Parameters);
            }
            catch (Exception ex)
            {
                OnNavigationFailed(navigationContext, target, ex);
            }
        }

        foreach (var target in GetAwareTargets(view).OfType<INavigationAware>())
        {
            try
            {
                target.OnNavigatedTo(navigationContext);
            }
            catch (Exception ex)
            {
                OnNavigationFailed(navigationContext, target, ex);
            }
        }
    }

    private async Task NotifyRepeatedNavigationAsync(object view, NavigationContext navigationContext)
    {
        await InitializeViewAsync(view, navigationContext);

        foreach (var target in GetAwareTargets(view).OfType<IInitialize>())
        {
            try
            {
                target.Initialize(navigationContext.Parameters);
            }
            catch (Exception ex)
            {
                OnNavigationFailed(navigationContext, target, ex);
            }
        }

        foreach (var target in GetAwareTargets(view).OfType<INavigationAware>())
        {
            try
            {
                target.OnNavigatedTo(navigationContext);
            }
            catch (Exception ex)
            {
                OnNavigationFailed(navigationContext, target, ex);
            }
        }
    }

    private async Task RunNavigationExitAnimationAsync(
        string regionName,
        Control regionTarget,
        IRegion region,
        object fromView,
        object? toView,
        NavigationContext navigationContext)
    {
        var context = new RegionNavigationAnimationContext(
            regionName,
            region,
            regionTarget,
            fromView,
            toView,
            navigationContext,
            RegionNavigationAnimationPhase.Exit);

        var pageAnimation = GetPageAnimation(fromView, context);
        if (pageAnimation is not null)
        {
            await RunAnimationAsync(pageAnimation, context, AnimateExitAsync);
            return;
        }

        var regionAnimation = GetRegionAnimation(context);
        if (regionAnimation is not null)
        {
            await RunAnimationAsync(regionAnimation, context, AnimateExitAsync);
        }
    }

    private async Task RunNavigationEnterAnimationAsync(
        string regionName,
        Control regionTarget,
        IRegion region,
        object? fromView,
        object toView,
        NavigationContext navigationContext)
    {
        var context = new RegionNavigationAnimationContext(
            regionName,
            region,
            regionTarget,
            fromView,
            toView,
            navigationContext,
            RegionNavigationAnimationPhase.Enter);

        var pageAnimation = GetPageAnimation(toView, context);
        try
        {
            if (pageAnimation is not null)
            {
                await RunAnimationAsync(pageAnimation, context, AnimateEnterAsync);
                return;
            }

            var regionAnimation = GetRegionAnimation(context);
            if (regionAnimation is not null)
            {
                await RunAnimationAsync(regionAnimation, context, AnimateEnterAsync);
            }
        }
        finally
        {
            EnsureViewVisible(toView);
        }
    }

    private void PrepareContentReplacementAnimation(
        string regionName,
        Control regionTarget,
        IRegion region,
        object? fromView,
        object? toView,
        NavigationContext? navigationContext)
    {
        if (regionTarget is not ContentControl)
        {
            return;
        }

        var context = new RegionNavigationAnimationContext(
            regionName,
            region,
            regionTarget,
            fromView,
            toView,
            navigationContext,
            RegionNavigationAnimationPhase.ContentReplacing);

        var pageAnimation = GetPageAnimation(toView, context) ?? GetPageAnimation(fromView, context);
        if (pageAnimation is not null)
        {
            RunAnimation(pageAnimation, context, PrepareContentReplacement);
            return;
        }

        var regionAnimation = GetRegionAnimation(context);
        if (regionAnimation is not null)
        {
            RunAnimation(regionAnimation, context, PrepareContentReplacement);
        }
    }

    private IRegionNavigationAnimationHandler? GetPageAnimation(object? view, RegionNavigationAnimationContext context)
    {
        if (view is null)
        {
            return null;
        }

        foreach (var animation in GetAwareTargets(view).OfType<IRegionViewNavigationAnimation>())
        {
            try
            {
                if (IsAnimationEnabled(animation, context))
                {
                    return animation;
                }
            }
            catch (Exception ex)
            {
                ReportAnimationFailure(context, animation, ex);
            }
        }

        return null;
    }

    private IRegionNavigationAnimationHandler? GetRegionAnimation(RegionNavigationAnimationContext context)
    {
        IRegionNavigationAnimation? animation = null;
        if (_container.IsRegistered(typeof(IRegionNavigationAnimation), context.RegionName))
        {
            animation = (IRegionNavigationAnimation)_container.Resolve(typeof(IRegionNavigationAnimation),
                context.RegionName);
        }
        else if (_container.IsRegistered(typeof(IRegionNavigationAnimation)))
        {
            animation = _container.Resolve<IRegionNavigationAnimation>();
        }

        if (animation is null)
        {
            return null;
        }

        try
        {
            return IsAnimationEnabled(animation, context) ? animation : null;
        }
        catch (Exception ex)
        {
            ReportAnimationFailure(context, animation, ex);
            return null;
        }
    }

    private async Task RunAnimationAsync(
        IRegionNavigationAnimationHandler animation,
        RegionNavigationAnimationContext context,
        Func<IRegionNavigationAnimationHandler, RegionNavigationAnimationContext, Task> action)
    {
        try
        {
            await action(animation, context);
        }
        catch (Exception ex)
        {
            ReportAnimationFailure(context, animation, ex);
        }
    }

    private void RunAnimation(
        IRegionNavigationAnimationHandler animation,
        RegionNavigationAnimationContext context,
        Action<IRegionNavigationAnimationHandler, RegionNavigationAnimationContext> action)
    {
        try
        {
            action(animation, context);
        }
        catch (Exception ex)
        {
            ReportAnimationFailure(context, animation, ex);
        }
    }

    private static bool IsAnimationEnabled(IRegionNavigationAnimationHandler animation,
        RegionNavigationAnimationContext context)
    {
        return animation switch
        {
            IRegionViewNavigationAnimation viewAnimation => viewAnimation.IsEnabled(context),
            IRegionNavigationAnimation regionAnimation => regionAnimation.IsEnabled(context),
            _ => false
        };
    }

    private static Task AnimateExitAsync(IRegionNavigationAnimationHandler animation,
        RegionNavigationAnimationContext context)
    {
        return animation switch
        {
            IRegionViewNavigationAnimation viewAnimation => viewAnimation.AnimateExitAsync(context),
            IRegionNavigationAnimation regionAnimation => regionAnimation.AnimateExitAsync(context),
            _ => Task.CompletedTask
        };
    }

    private static Task AnimateEnterAsync(IRegionNavigationAnimationHandler animation,
        RegionNavigationAnimationContext context)
    {
        return animation switch
        {
            IRegionViewNavigationAnimation viewAnimation => viewAnimation.AnimateEnterAsync(context),
            IRegionNavigationAnimation regionAnimation => regionAnimation.AnimateEnterAsync(context),
            _ => Task.CompletedTask
        };
    }

    private static void PrepareContentReplacement(IRegionNavigationAnimationHandler animation,
        RegionNavigationAnimationContext context)
    {
        switch (animation)
        {
            case IRegionViewNavigationAnimation viewAnimation:
                viewAnimation.PrepareContentReplacement(context);
                break;
            case IRegionNavigationAnimation regionAnimation:
                regionAnimation.PrepareContentReplacement(context);
                break;
        }
    }

    private void ReportAnimationFailure(RegionNavigationAnimationContext context, object target, Exception exception)
    {
        EnsureViewVisible(context.ToView);

        if (context.NavigationContext is not null)
        {
            try
            {
                GetNavigationLogger().NavigationFailed(context.NavigationContext, exception);
            }
            catch
            {
            }

            OnNavigationFailed(context.NavigationContext, target, exception);
        }
    }

    private static void EnsureViewVisible(object? view)
    {
        if (view is not Control control)
        {
            return;
        }

        control.Opacity = 1;
        control.RenderTransform = null;
    }

    private async Task InitializeViewAsync(object view, NavigationContext navigationContext)
    {
        foreach (var target in GetAwareTargets(view).OfType<IInitializeAsync>())
        {
            try
            {
                await target.InitializeAsync(navigationContext.Parameters).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OnNavigationFailed(navigationContext, target, ex);
                throw;
            }
        }
    }

    private void OnNavigationFailed(NavigationContext navigationContext, object? target, Exception exception)
    {
        NavigationFailed?.Invoke(this, new RegionNavigationFailedEventArgs(navigationContext, target, exception));
    }

    private void NotifyDestroy(object view)
    {
        foreach (var target in GetAwareTargets(view))
        {
            try
            {
                if (target is IDestructible destructible)
                {
                    destructible.Destroy();
                }

                if (target is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                if (target is IAsyncDisposable asyncDisposable)
                {
                    _ = DisposeAsyncSilently(asyncDisposable);
                }
            }
            catch
            {
            }
        }
    }

    private static async Task DisposeAsyncSilently(IAsyncDisposable asyncDisposable)
    {
        try
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
        }
    }

    internal void NotifyDestroyIfNeeded(object view)
    {
        if (ShouldKeepAlive(view))
        {
            return;
        }

        NotifyDestroy(view);
    }

    private IRegionNavigationLogger GetNavigationLogger()
    {
        return _container.IsRegistered(typeof(IRegionNavigationLogger))
            ? _container.Resolve<IRegionNavigationLogger>()
            : NullRegionNavigationLogger.Instance;
    }

    private static void SetViewActiveMarker(object view, bool isActive)
    {
        if (view is Control control)
        {
            SetIsActiveView(control, isActive);
        }
    }

    private static void SetActiveRegionState(Control regionTarget, IRegion region, object? activeView)
    {
        SetActiveView(regionTarget, activeView);
        SetActiveIndex(regionTarget, activeView is null ? -1 : region.Views.IndexOf(activeView));
    }

    private static void RefreshActiveRegionState(Control regionTarget, IRegion region)
    {
        SetActiveRegionState(regionTarget, region, region.ActiveViews.FirstOrDefault());
    }

    private bool ShouldKeepAlive(object view)
    {
        var lifetimeTarget = GetAwareTargets(view)
            .OfType<IRegionMemberLifetime>()
            .FirstOrDefault();

        return lifetimeTarget?.KeepAlive == true;
    }

    private IReadOnlyList<object> GetAwareTargets(object view)
    {
        var dataContext = view is Control control ? control.DataContext : null;
        var cache = _awareTargetCache.GetOrCreateValue(view);
        return cache.GetTargets(view, dataContext);
    }

    internal static IEnumerable<object> GetAwareTargets(params object?[] targets)
    {
        foreach (var target in targets)
        {
            if (target is not null)
            {
                yield return target;
            }
        }
    }

    internal void RegisterRegions(Control root)
    {
        foreach (var control in EnumerateRegionCandidates(root))
        {
            RegisterRegionIfNamed(control);
        }
    }

    private static IEnumerable<Control> EnumerateRegionCandidates(Control root)
    {
        var visited = new HashSet<Control>();
        var stack = new Stack<Control>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var control = stack.Pop();
            if (!visited.Add(control))
            {
                continue;
            }

            yield return control;

            foreach (var child in GetRegionCandidateChildren(control))
            {
                stack.Push(child);
            }
        }
    }

    private static IEnumerable<Control> GetRegionCandidateChildren(Control control)
    {
        if (control is Panel panel)
        {
            foreach (var childControl in panel.Children.OfType<Control>())
            {
                yield return childControl;
            }
        }

        foreach (var logicalChild in control.GetLogicalChildren().OfType<Control>())
        {
            yield return logicalChild;
        }

        if (control is ContentControl { Content: Control contentControl })
        {
            yield return contentControl;
        }
    }

    private void RegisterRegionIfNamed(Control control)
    {
        if (GetRegionName(control) is { Length: > 0 } regionName)
        {
            RegisterRegion(control, regionName);
        }
    }

    private static void RegisterRegionWhenAttached(object? sender, VisualTreeAttachmentEventArgs args)
    {
        if (sender is not Control control)
        {
            return;
        }

        control.AttachedToVisualTree -= RegisterRegionWhenAttached;
        if (GetRegionName(control) is { Length: > 0 } regionName &&
            FindRegionManager(control) is RegionManager regionManager)
        {
            regionManager.RegisterRegion(control, regionName);
        }
    }

    private static IRegionManager? FindRegionManager(AvaloniaObject target)
    {
        if (GetRegionManager(target) is { } localManager)
        {
            return localManager;
        }

        if (target is StyledElement styledElement)
        {
            foreach (var ancestor in styledElement.GetLogicalAncestors().OfType<AvaloniaObject>())
            {
                if (GetRegionManager(ancestor) is { } logicalManager)
                {
                    return logicalManager;
                }
            }
        }

        if (target is Visual visual)
        {
            foreach (var ancestor in visual.GetVisualAncestors().OfType<AvaloniaObject>())
            {
                if (GetRegionManager(ancestor) is { } visualManager)
                {
                    return visualManager;
                }
            }
        }

        return null;
    }

    public static string? GetRegionName(Control control) => control.GetValue(RegionNameProperty);

    public static void SetRegionName(Control control, string? value) => control.SetValue(RegionNameProperty, value);

    public static object? GetRegionContext(Control control) => control.GetValue(RegionContextProperty);

    public static void SetRegionContext(Control control, object? value) =>
        control.SetValue(RegionContextProperty, value);

    public static IRegionManager? GetRegionManager(AvaloniaObject target) => target.GetValue(RegionManagerProperty);

    public static bool GetCreateRegionManagerScope(Control control) =>
        control.GetValue(CreateRegionManagerScopeProperty);

    public static void SetCreateRegionManagerScope(Control control, bool value) =>
        control.SetValue(CreateRegionManagerScopeProperty, value);

    public static bool GetIsActiveView(Control control) => control.GetValue(IsActiveViewProperty);

    public static void SetIsActiveView(Control control, bool value) => control.SetValue(IsActiveViewProperty, value);

    public static object? GetActiveView(Control control) => control.GetValue(ActiveViewProperty);

    public static void SetActiveView(Control control, object? value) => control.SetValue(ActiveViewProperty, value);

    public static int GetActiveIndex(Control control) => control.GetValue(ActiveIndexProperty);

    public static void SetActiveIndex(Control control, int value) => control.SetValue(ActiveIndexProperty, value);

    public static void SetRegionManager(AvaloniaObject target, IRegionManager? value)
    {
        target.SetValue(RegionManagerProperty, value);
    }

    public static void UpdateRegions(AvaloniaObject target)
    {
        if (FindRegionManager(target) is RegionManager regionManager && target is Control control)
        {
            regionManager.RegisterRegions(control);
            regionManager.UpdateRegisteredRegions();
        }
    }

    public static void UpdateRegions()
    {
    }
}
