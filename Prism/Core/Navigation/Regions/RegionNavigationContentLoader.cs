using Avalonia.Controls;
using Prism.Ioc;

namespace Prism.Navigation.Regions;

public sealed class RegionNavigationContentLoader : IRegionNavigationContentLoader
{
    private readonly IRegionViewFactory _viewFactory;

    public RegionNavigationContentLoader(IContainerProvider container)
    {
        _viewFactory = container.IsRegistered(typeof(IRegionViewFactory))
            ? container.Resolve<IRegionViewFactory>()
            : new ContainerRegionViewFactory(container);
    }

    public object LoadContent(IRegion region, NavigationContext navigationContext)
    {
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(navigationContext);

        var candidates = GetTargetCandidates(navigationContext.Uri);
        foreach (var view in region.Views)
        {
            if (candidates.Any(candidate => IsViewMatch(view, candidate)) &&
                IsNavigationTarget(view, navigationContext))
            {
                return view;
            }
        }

        foreach (var candidate in candidates)
        {
            try
            {
                return _viewFactory.CreateView(candidate);
            }
            catch (ContainerResolutionException)
            {
            }
        }

        throw new InvalidOperationException($"Navigation target '{string.Join("', '", candidates)}' is not registered.");
    }

    private static IReadOnlyList<string> GetTargetCandidates(Uri uri)
    {
        var source = uri.OriginalString;
        var target = uri.IsAbsoluteUri ? uri.AbsolutePath : source;
        var queryIndex = target.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
        {
            target = target[..queryIndex];
        }

        target = target.Trim('/');
        var slashName = target.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        var dotName = slashName?.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        var candidates = new List<string>(4);
        AddCandidate(candidates, source);
        AddCandidate(candidates, target);
        AddCandidate(candidates, slashName);
        AddCandidate(candidates, dotName);
        return candidates;
    }

    private static void AddCandidate(List<string> candidates, string? candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate) && !candidates.Contains(candidate, StringComparer.Ordinal))
        {
            candidates.Add(candidate);
        }
    }

    private static bool IsViewMatch(object view, string targetName)
    {
        var viewType = view.GetType();
        return string.Equals(viewType.Name, targetName, StringComparison.Ordinal) ||
               string.Equals(viewType.FullName, targetName, StringComparison.Ordinal) ||
               string.Equals(viewType.AssemblyQualifiedName, targetName, StringComparison.Ordinal);
    }

    private static bool IsNavigationTarget(object view, NavigationContext navigationContext)
    {
        foreach (var target in RegionManager.GetAwareTargets(view, view is Control control ? control.DataContext : null).OfType<INavigationAware>())
        {
            if (!target.IsNavigationTarget(navigationContext))
            {
                return false;
            }
        }

        return true;
    }
}
