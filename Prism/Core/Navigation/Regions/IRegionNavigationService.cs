using System;
using System.Threading.Tasks;

namespace Prism.Navigation.Regions;

public interface IRegionNavigationService
{
    IRegion? Region { get; set; }

    IRegionNavigationJournal Journal { get; }

    event EventHandler<RegionNavigationFailedEventArgs>? NavigationFailed;

    void RequestNavigate(Uri target, Action<NavigationResult>? navigationCallback, INavigationParameters? navigationParameters);

    Task<NavigationResult> RequestNavigateAsync(Uri target, INavigationParameters? navigationParameters = null);
}