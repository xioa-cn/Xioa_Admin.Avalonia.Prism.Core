namespace Prism.Navigation.Regions;

public sealed class RegionMemberLifetimeBehavior : IRegionBehavior
{
    public IRegion Region { get; set; } = null!;

    public void Attach()
    {
        Region.ViewRemoved += (_, args) =>
        {
            if (Region.RegionManager is RegionManager regionManager && regionManager.IsMovingView(args.View))
            {
                return;
            }

            if (Region.RegionManager is RegionManager manager)
            {
                manager.NotifyDestroyIfNeeded(args.View);
            }
        };
    }
}