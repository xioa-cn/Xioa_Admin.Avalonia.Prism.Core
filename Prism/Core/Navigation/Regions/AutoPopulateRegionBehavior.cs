namespace Prism.Navigation.Regions;

public sealed class AutoPopulateRegionBehavior : IRegionBehavior
{
    private readonly IRegionViewRegistry? _regionViewRegistry;

    public AutoPopulateRegionBehavior()
    {
    }

    public AutoPopulateRegionBehavior(IRegionViewRegistry regionViewRegistry)
    {
        _regionViewRegistry = regionViewRegistry;
    }

    public IRegion Region { get; set; } = null!;

    public void Attach()
    {
        if (_regionViewRegistry is not null)
        {
            foreach (var getView in _regionViewRegistry.GetContents(Region.Name))
            {
                AddView(getView);
            }

            _regionViewRegistry.ContentRegistered += OnContentRegistered;
        }

        foreach (var view in Region.Views)
        {
            if (!Region.ActiveViews.Contains(view))
            {
                Region.Activate(view);
            }
        }
    }

    private void OnContentRegistered(object? sender, ViewRegisteredEventArgs args)
    {
        if (string.Equals(args.RegionName, Region.Name, StringComparison.Ordinal))
        {
            AddView(args.GetView);
        }
    }

    private void AddView(Func<object> getView)
    {
        var view = getView();
        if (!Region.Views.Contains(view))
        {
            Region.Add(view);
        }
    }
}