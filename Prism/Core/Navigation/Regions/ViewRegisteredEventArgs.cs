namespace Prism.Navigation.Regions;

public sealed class ViewRegisteredEventArgs : EventArgs
{
    public ViewRegisteredEventArgs(string regionName, Func<object> getView)
    {
        RegionName = regionName;
        GetView = getView;
    }

    public string RegionName { get; }

    public Func<object> GetView { get; }
}