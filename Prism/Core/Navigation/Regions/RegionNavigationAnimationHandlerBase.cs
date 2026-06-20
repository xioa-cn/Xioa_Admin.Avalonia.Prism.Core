using System.Threading.Tasks;

namespace Prism.Navigation.Regions;

public abstract class RegionNavigationAnimationHandlerBase : IRegionNavigationAnimationHandler
{
    public virtual bool IsEnabled(RegionNavigationAnimationContext context) => true;

    public virtual Task AnimateExitAsync(RegionNavigationAnimationContext context) => Task.CompletedTask;

    public virtual Task AnimateEnterAsync(RegionNavigationAnimationContext context) => Task.CompletedTask;

    public virtual void PrepareContentReplacement(RegionNavigationAnimationContext context)
    {
    }
}