namespace Prism.Navigation.Regions;

public interface IRegionViewNavigationAnimation : IRegionNavigationAnimationHandler
{
    bool IsEnabled(RegionNavigationAnimationContext context);

    Task AnimateExitAsync(RegionNavigationAnimationContext context);

    Task AnimateEnterAsync(RegionNavigationAnimationContext context);

    void PrepareContentReplacement(RegionNavigationAnimationContext context);
}