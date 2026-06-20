namespace Prism.Navigation.Regions;

public sealed class NullRegionNavigationLogger : IRegionNavigationLogger
{
    public static readonly NullRegionNavigationLogger Instance = new();

    private NullRegionNavigationLogger()
    {
    }

    public void NavigationStarting(NavigationContext navigationContext)
    {
    }

    public void NavigationSucceeded(NavigationContext navigationContext, object view)
    {
    }

    public void NavigationCanceled(NavigationContext navigationContext)
    {
    }

    public void NavigationFailed(NavigationContext navigationContext, Exception exception)
    {
    }

    public void ViewDestroyed(object view)
    {
    }
}