using System;

namespace Prism.Navigation.Regions;

public sealed class RegionManagerRegistrationBehavior : IRegionBehavior
{
    public IRegion Region { get; set; } = null!;

    public void Attach()
    {
        if (Region.RegionManager is null)
        {
            throw new InvalidOperationException($"Region '{Region.Name}' must be attached to a region manager.");
        }
    }
}