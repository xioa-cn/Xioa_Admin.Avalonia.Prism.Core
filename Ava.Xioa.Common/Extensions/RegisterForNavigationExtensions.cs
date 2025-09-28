using System.Linq;
using System.Reflection;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Models;
using Prism.Ioc;

namespace Ava.Xioa.Common.Extensions;

public static class RegisterForNavigationExtensions
{
    public static IContainerRegistry RegisterForNavigations(this IContainerRegistry containerRegistry,
        Assembly assembly)
    {
        var types = assembly.GetTypes().Where(t => t.GetCustomAttribute<RegisterForNavigationAttribute>() != null)
            .ToList();

        if (types.Count <= 0) return containerRegistry;

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<RegisterForNavigationAttribute>();
            if (attr == null) continue;
            
            if (attr.Version == ProgrammingVersion.Obsolete)
            {
                continue;
            }

            containerRegistry.RegisterForNavigation(type, attr.NavigationName);
        }

        return containerRegistry;
    }
}