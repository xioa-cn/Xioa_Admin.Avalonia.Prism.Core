namespace Prism.Navigation.Regions;

public sealed class RegionNavigationFailedEventArgs : EventArgs
{
    public RegionNavigationFailedEventArgs(NavigationContext navigationContext, object? target, Exception exception)
    {
        NavigationContext = navigationContext;
        Target = target;
        Exception = exception;
    }

    public NavigationContext NavigationContext { get; }

    public object? Target { get; }

    public Exception Exception { get; }
}