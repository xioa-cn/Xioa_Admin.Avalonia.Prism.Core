using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Utils;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

namespace Ava.Xioa.Common.Extensions;

public static class RegistryPrismAutoRepositoryExtensions
{
    public static IContainerRegistry AddPrismAutoRepository(this IContainerRegistry serviceCollection)
    {
        var compilationLibrary = DependencyCompilation.GetCompilationLibrary();
        if (compilationLibrary == null) return serviceCollection;
        foreach (var compilation in compilationLibrary)
        {
            var types = AssemblyLoadContext.Default
                .LoadFromAssemblyName(new AssemblyName(compilation.Name))
                .GetTypes().Where(a =>
                    a.GetCustomAttribute<AutoRepositoryAttribute>() != null)
                .ToList();
            if (types.Count <= 0) continue;
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<AutoRepositoryAttribute>();
                if (attr == null) continue;
                switch (attr.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        serviceCollection.RegisterScoped(attr.Type, type);
                        break;
                    case ServiceLifetime.Singleton:
                        serviceCollection.RegisterSingleton(attr.Type, type);
                        break;
                    case ServiceLifetime.Transient:
                        serviceCollection.Register(attr.Type, type);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        return serviceCollection;
    }
}