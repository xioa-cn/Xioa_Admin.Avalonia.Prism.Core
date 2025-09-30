using System.Linq;
using System.Reflection;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Models;
using Prism.Navigation.Regions;

namespace Ava.Xioa.Common.Extensions;

public static class RegisterViewWithRegionExtensions
{
    public static IRegionManager RegisterViewsWithRegion(this IRegionManager regionManager, Assembly assembly)
    {
        var types = assembly.GetTypes().Where(t => t.GetCustomAttribute<PrismRegisterForNavigationAttribute>() != null)
            .OrderByDescending(item => item.GetCustomAttribute<PrismRegisterForNavigationAttribute>()?.ZIndex ?? -1).ToList();

        if (types.Count <= 0) return regionManager;

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<PrismRegisterForNavigationAttribute>();
            if (attr == null) continue;

            if (attr.Version == ProgrammingVersion.Obsolete)
            {
                continue;
            }

            regionManager.RegisterViewWithRegion(attr.Region, type);
        }

        return regionManager;
    }
}