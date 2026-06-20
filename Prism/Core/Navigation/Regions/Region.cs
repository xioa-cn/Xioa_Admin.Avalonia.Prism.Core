using System;

namespace Prism.Navigation.Regions;

public sealed class Region : IRegion
{
    private readonly ViewsCollection _views = new();
    private readonly ViewsCollection _activeViews = new();
    private readonly RegionNavigationJournal _navigationJournal;
    private WeakReference<IRegionManager>? _regionManager;

    public Region()
    {
        _navigationJournal = new RegionNavigationJournal(this);
    }

    public event EventHandler<RegionViewChangedEventArgs>? ViewAdded;

    public event EventHandler<RegionViewChangedEventArgs>? ViewRemoved;

    public event EventHandler<RegionViewChangedEventArgs>? ViewActivated;

    public event EventHandler<RegionViewChangedEventArgs>? ViewDeactivated;

    public string Name { get; set; } = string.Empty;

    public IViewsCollection Views => _views;

    public IViewsCollection ActiveViews => _activeViews;

    public object? Context { get; set; }

    public IRegionManager? RegionManager
    {
        get
        {
            if (_regionManager is null)
            {
                return null;
            }

            return _regionManager.TryGetTarget(out var regionManager) ? regionManager : null;
        }
        set
        {
            _regionManager = value is null ? null : new WeakReference<IRegionManager>(value);
        }
    }

    public IRegionNavigationJournal NavigationJournal => _navigationJournal;

    internal RegionNavigationJournal InternalNavigationJournal => _navigationJournal;

    public void Add(object view)
    {
        Add(view, view.GetType().Name);
    }

    public void Add(object view, string viewName)
    {
        if (_views.Add(view))
        {
            ViewAdded?.Invoke(this, new RegionViewChangedEventArgs(this, view));
        }
    }

    public void Activate(object view)
    {
        if (_views.Add(view))
        {
            ViewAdded?.Invoke(this, new RegionViewChangedEventArgs(this, view));
        }

        if (_activeViews.Add(view))
        {
            ViewActivated?.Invoke(this, new RegionViewChangedEventArgs(this, view));
        }
    }

    public void Deactivate(object view)
    {
        if (_activeViews.Remove(view))
        {
            ViewDeactivated?.Invoke(this, new RegionViewChangedEventArgs(this, view));
        }
    }

    public void Remove(object view)
    {
        Deactivate(view);
        if (_views.Remove(view))
        {
            ViewRemoved?.Invoke(this, new RegionViewChangedEventArgs(this, view));
        }
    }
}