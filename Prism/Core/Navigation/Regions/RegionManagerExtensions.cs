using Avalonia.Controls;

namespace Prism.Navigation.Regions;


public static class RegionManagerExtensions
{
    public static IRegionManager RegisterViewWithRegion<TView>(this IRegionManager regionManager, string regionName)
    {
        return regionManager.RegisterViewWithRegion(regionName, typeof(TView));
    }

    [Obsolete("Use RequestNavigateAsync instead.")]
    public static void RequestNavigate(this IRegionManager regionManager, string regionName, string source, INavigationParameters navigationParameters)
    {
        regionManager.RequestNavigate(regionName, source, null, navigationParameters);
    }

    [Obsolete("Use RequestNavigateAsync instead.")]
    public static void RequestNavigate(this IRegionManager regionManager, string regionName, Uri source, INavigationParameters navigationParameters)
    {
        regionManager.RequestNavigate(regionName, source, null, navigationParameters);
    }

    public static void RegisterDefaultRegionBehaviors(this IRegionBehaviorFactory regionBehaviors)
    {
        regionBehaviors.AddIfMissing(nameof(RegionManagerRegistrationBehavior), typeof(RegionManagerRegistrationBehavior));
        regionBehaviors.AddIfMissing(nameof(AutoPopulateRegionBehavior), typeof(AutoPopulateRegionBehavior));
        regionBehaviors.AddIfMissing(nameof(RegionActiveAwareBehavior), typeof(RegionActiveAwareBehavior));
        regionBehaviors.AddIfMissing(nameof(RegionMemberLifetimeBehavior), typeof(RegionMemberLifetimeBehavior));
    }

    public static void RegisterDefaultRegionAdapterMappings(this RegionAdapterMappings regionAdapterMappings)
    {
        regionAdapterMappings.RegisterMapping(typeof(ContentControl), typeof(ContentControlRegionAdapter));
        regionAdapterMappings.RegisterMapping(typeof(Panel), typeof(PanelRegionAdapter));
        regionAdapterMappings.RegisterMapping(typeof(ItemsControl), typeof(ItemsControlRegionAdapter));
    }
}