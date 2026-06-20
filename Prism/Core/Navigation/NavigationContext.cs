namespace Prism.Navigation;

public sealed class NavigationContext
{
    public NavigationContext(string regionName, Uri uri, INavigationParameters parameters)
    {
        RegionName = regionName;
        Uri = uri;
        Parameters = parameters.AsReadOnly();
    }

    public string RegionName { get; }

    public Uri Uri { get; }

    public INavigationParameters Parameters { get; }
}
