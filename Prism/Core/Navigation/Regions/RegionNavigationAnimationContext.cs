using Avalonia.Controls;

namespace Prism.Navigation.Regions;

public sealed class RegionNavigationAnimationContext
{
    public RegionNavigationAnimationContext(
        string regionName,
        IRegion region,
        Control regionTarget,
        object? fromView,
        object? toView,
        NavigationContext? navigationContext,
        RegionNavigationAnimationPhase phase)
    {
        RegionName = regionName;
        Region = region;
        RegionTarget = regionTarget;
        FromView = fromView;
        ToView = toView;
        NavigationContext = navigationContext;
        Phase = phase;
    }

    public string RegionName { get; }

    public IRegion Region { get; }

    public Control RegionTarget { get; }

    public object? FromView { get; }

    public object? ToView { get; }

    public NavigationContext? NavigationContext { get; }

    public RegionNavigationAnimationPhase Phase { get; }
}
