namespace Prism.Navigation.Regions;

public interface IRegion
{
    event EventHandler<RegionViewChangedEventArgs>? ViewAdded;

    event EventHandler<RegionViewChangedEventArgs>? ViewRemoved;

    event EventHandler<RegionViewChangedEventArgs>? ViewActivated;

    event EventHandler<RegionViewChangedEventArgs>? ViewDeactivated;

    string Name { get; set; }

    IViewsCollection Views { get; }

    IViewsCollection ActiveViews { get; }

    object? Context { get; set; }

    IRegionManager? RegionManager { get; set; }

    IRegionNavigationJournal NavigationJournal { get; }

    void Add(object view);

    void Add(object view, string viewName);

    void Activate(object view);

    void Deactivate(object view);

    void Remove(object view);
}