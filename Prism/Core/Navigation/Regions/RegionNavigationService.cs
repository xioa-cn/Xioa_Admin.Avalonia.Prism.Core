namespace Prism.Navigation.Regions;

public sealed class RegionNavigationService : IRegionNavigationService
{
    private readonly IRegionManager _regionManager;
    private IRegion? _region;

    public RegionNavigationService(IRegionManager regionManager)
    {
        _regionManager = regionManager;
        _regionManager.NavigationFailed += (_, args) => NavigationFailed?.Invoke(this, args);
    }

    public IRegion? Region
    {
        get => _region;
        set => _region = value;
    }

    public IRegionNavigationJournal Journal =>
        Region?.NavigationJournal ?? throw new InvalidOperationException("The navigation service is not attached to a region.");

    public event EventHandler<RegionNavigationFailedEventArgs>? NavigationFailed;

    public void RequestNavigate(Uri target, Action<NavigationResult>? navigationCallback, INavigationParameters? navigationParameters)
    {
        _ = RequestNavigateAsync(target, navigationParameters)
            .ContinueWith(task =>
            {
                var result = task.Status == TaskStatus.RanToCompletion
                    ? task.Result
                    : new NavigationResult(task.Exception?.GetBaseException() ?? new InvalidOperationException("Navigation failed."));
                navigationCallback?.Invoke(result);
            }, TaskScheduler.Default);
    }

    public Task<NavigationResult> RequestNavigateAsync(Uri target, INavigationParameters? navigationParameters = null)
    {
        if (Region is null)
        {
            return Task.FromResult(new NavigationResult(new InvalidOperationException("The navigation service is not attached to a region.")));
        }

        return _regionManager.RequestNavigateAsync(Region.Name, target, navigationParameters);
    }
}
