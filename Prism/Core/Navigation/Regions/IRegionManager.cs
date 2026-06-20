using System;
using System.Threading.Tasks;
using Prism.Ioc;

namespace Prism.Navigation.Regions;

public interface IRegionManager
{
    event EventHandler<RegionNavigationFailedEventArgs>? NavigationFailed;

    event EventHandler<RegionChangedEventArgs>? RegionRegistered;

    event EventHandler<RegionChangedEventArgs>? RegionRemoved;

    TimeSpan NavigationTimeout { get; set; }

    int MaxCompletedNavigationOnceKeyCount { get; set; }

    IRegionCollection Regions { get; }

    IRegionManager CreateRegionManager();

    IRegionManager AddToRegion(string regionName, object view);

    IRegionManager AddToRegion(string regionName, string viewName);

    IRegionManager RegisterViewWithRegion(string regionName, string viewName);

    IRegionManager RegisterViewWithRegion(string regionName, Type viewType);

    IRegionManager RegisterViewWithRegion(string regionName, Func<IContainerProvider, object> getContentDelegate);

    IRegionManager RegisterViewWithRegion(string regionName, Func<object> getContentDelegate);

    IRegionManager AddNavigationInterceptor(INavigationInterceptor interceptor);

    IRegionManager RegisterNavigationAlias(string alias, string target);

    IRegionManager RegisterNavigationRoute(string routeTemplate, string target);

    IRegionManager RegisterNavigationRoute(string routeTemplate, string target, Func<INavigationParameters, bool> constraint);

    Task<NavigationResult> RequestNavigateOnceAsync(string regionName, string source, INavigationParameters? navigationParameters = null);

    [Obsolete("Use RequestNavigateAsync instead. Synchronous region navigation can block UI threads and cannot reliably return the newly navigated view.")]
    object? RequestNavigate(string regionName, string source);

    [Obsolete("Use RequestNavigateAsync instead.")]
    void RequestNavigate(string regionName, string source, Action<NavigationResult>? navigationCallback);

    [Obsolete("Use RequestNavigateAsync instead.")]
    void RequestNavigate(string regionName, string source, Action<NavigationResult>? navigationCallback, INavigationParameters? navigationParameters);

    [Obsolete("Use RequestNavigateAsync instead.")]
    void RequestNavigate(string regionName, Uri source, Action<NavigationResult>? navigationCallback);

    [Obsolete("Use RequestNavigateAsync instead.")]
    void RequestNavigate(string regionName, Uri source, Action<NavigationResult>? navigationCallback, INavigationParameters? navigationParameters);

    Task<NavigationResult> RequestNavigateAsync(string regionName, string source, INavigationParameters? navigationParameters = null);

    Task<NavigationResult> RequestNavigateAsync(string regionName, Uri source, INavigationParameters? navigationParameters = null);
}