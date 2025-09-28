using System.Reflection;
using Prism.Navigation.Regions;

namespace Ava.Xioa.Common.Extensions;

public static class RegisterViewWithRegionExtensions
{
    public static IRegionManager RegisterViewsWithRegion(this IRegionManager regionManager, Assembly assembly)
    {
        return regionManager;
    }
}