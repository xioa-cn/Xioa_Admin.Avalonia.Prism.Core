using System.Threading.Tasks;
using Ava.Xioa.Common.Services;
using Prism.Navigation.Regions;

namespace Ava.Xioa.InfrastructureModule.Animation;

public class HomeRegionAnimation : RegionNavigationAnimationBase
{
    public override Task AnimateExitAsync(RegionNavigationAnimationContext context)
    {
        return NavigationAnimationRunner.ScaleFadeAsync(
            context.FromView,
            fromOpacity: 1,
            toOpacity: 0,
            fromScale: 1,
            toScale: 0.97,
            milliseconds: 130);
    }

    public override Task AnimateEnterAsync(RegionNavigationAnimationContext context)
    {
        return NavigationAnimationRunner.ScaleFadeAsync(
            context.ToView,
            fromOpacity: 0,
            toOpacity: 1,
            fromScale: 0.96,
            toScale: 1,
            milliseconds: 240);
    }

    public override void PrepareContentReplacement(RegionNavigationAnimationContext context)
    {
        NavigationAnimationRunner.PrepareScale(context.ToView, opacity: 0, scale: 0.96);
    }
}