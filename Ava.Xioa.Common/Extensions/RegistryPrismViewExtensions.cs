using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Utils;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

namespace Ava.Xioa.Common.Extensions;

public static class RegistryPrismViewExtensions
{
    public static IContainerRegistry AddPrismViews(this IContainerRegistry serviceCollection)
    {
        var compilationLibrary = DependencyCompilation.GetCompilationLibrary();
        if (compilationLibrary == null) return serviceCollection;
        foreach (var compilation in compilationLibrary)
        {
            var types = AssemblyLoadContext.Default
                .LoadFromAssemblyName(new AssemblyName(compilation.Name))
                .GetTypes().Where(a =>
                    a.GetCustomAttribute<PrismViewAttribute>() != null)
                .ToList();
            if (types.Count <= 0) continue;
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<PrismViewAttribute>();
                if (attr == null) continue;
                switch (attr.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        if (string.IsNullOrEmpty(attr.ServiceName))
                        {
                            serviceCollection.RegisterScoped(type, type);
                        }
                        else
                        {
                            serviceCollection.RegisterScoped(type, type);
                        }

                        break;
                    case ServiceLifetime.Singleton:
                        if (string.IsNullOrEmpty(attr.ServiceName))
                        {
                            serviceCollection.RegisterSingleton(type, type);
                        }
                        else
                        {
                            serviceCollection.RegisterSingleton(type, type, attr.ServiceName);
                        }

                        break;
                    case ServiceLifetime.Transient:
                        if (string.IsNullOrEmpty(attr.ServiceName))
                        {
                            serviceCollection.Register(type, type);
                        }
                        else
                        {
                            serviceCollection.Register(type, type, attr.ServiceName);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        return serviceCollection;
    }
}