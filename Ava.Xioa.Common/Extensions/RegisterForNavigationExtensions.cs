using System.Reflection;
using Prism.Ioc;

namespace Ava.Xioa.Common.Extensions;

public static class RegisterForNavigationExtensions
{
    public static IContainerRegistry RegisterForNavigations(this IContainerRegistry containerRegistry, Assembly assembly)
    {
        return containerRegistry;
    }
}