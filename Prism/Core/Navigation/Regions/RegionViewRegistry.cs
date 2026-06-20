using System;
using System.Collections.Generic;
using Prism.Ioc;

namespace Prism.Navigation.Regions;

public sealed class RegionViewRegistry : IRegionViewRegistry
{
    private readonly IRegionViewFactory _viewFactory;
    private readonly Dictionary<string, List<Func<object>>> _registeredViews = new(StringComparer.Ordinal);
    private readonly object _syncRoot = new();

    public RegionViewRegistry(IContainerProvider container)
    {
        _viewFactory = container.IsRegistered(typeof(IRegionViewFactory))
            ? container.Resolve<IRegionViewFactory>()
            : new ContainerRegionViewFactory(container);
    }

    public event EventHandler<ViewRegisteredEventArgs>? ContentRegistered;

    public IEnumerable<Func<object>> GetContents(string regionName)
    {
        lock (_syncRoot)
        {
            return _registeredViews.TryGetValue(regionName, out var views)
                ? views.ToArray()
                : Array.Empty<Func<object>>();
        }
    }

    public void RegisterViewWithRegion(string regionName, Type viewType)
    {
        RegisterViewWithRegion(regionName, () => _viewFactory.CreateView(viewType));
    }

    public void RegisterViewWithRegion(string regionName, Func<object> getContentDelegate)
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

        ContentRegistered?.Invoke(this, new ViewRegisteredEventArgs(regionName, getContentDelegate));
    }
}