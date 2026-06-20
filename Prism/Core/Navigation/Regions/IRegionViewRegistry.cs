namespace Prism.Navigation.Regions;

public interface IRegionViewRegistry
{
    event EventHandler<ViewRegisteredEventArgs>? ContentRegistered;

    IEnumerable<Func<object>> GetContents(string regionName);

    void RegisterViewWithRegion(string regionName, Type viewType);

    void RegisterViewWithRegion(string regionName, Func<object> getContentDelegate);
}