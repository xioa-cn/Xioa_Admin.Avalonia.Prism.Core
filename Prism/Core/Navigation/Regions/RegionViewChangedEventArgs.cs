namespace Prism.Navigation.Regions;

public sealed class RegionViewChangedEventArgs : EventArgs
{
    public RegionViewChangedEventArgs(IRegion region, object view)
    {
        Region = region;
        View = view;
    }

    public IRegion Region { get; }

    public object View { get; }
}