using Avalonia.Controls;

namespace Prism.Navigation.Regions;

public sealed class RegionActiveAwareBehavior : IRegionBehavior
{
    public IRegion Region { get; set; } = null!;

    public void Attach()
    {
        Region.ViewActivated += (_, args) => SetActiveAware(args.View, true);
        Region.ViewDeactivated += (_, args) => SetActiveAware(args.View, false);

        foreach (var view in Region.ActiveViews)
        {
            SetActiveAware(view, true);
        }
    }

    private static void SetActiveAware(object view, bool isActive)
    {
        foreach (var target in RegionManager.GetAwareTargets(view, view is Control control ? control.DataContext : null).OfType<IActiveAware>())
        {
            target.IsActive = isActive;
        }
    }
}