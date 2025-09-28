using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Prism.Ioc;

namespace Ava.Xioa.Common.Extensions;

public static class RegistryPrismServicesExtensions
{
    public static IContainerRegistry AddPrismServices(this IContainerRegistry serviceCollection)
    {
        var compilationLibrary = DependencyCompilation.GetCompilationLibrary();
        if (compilationLibrary == null) return serviceCollection;
        foreach (var compilation in compilationLibrary)
        {
            var types = AssemblyLoadContext.Default
                .LoadFromAssemblyName(new AssemblyName(compilation.Name))
                .GetTypes().Where(a =>
                    a.GetCustomAttribute<PrismServiceAttribute>() != null)
                .ToList();
            if (types.Count <= 0) continue;
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<PrismServiceAttribute>();
                if (attr == null) continue;
                
                if (attr.Version == ProgrammingVersion.Obsolete)
                {
                    continue;
                }
                
                switch (attr.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        if (string.IsNullOrEmpty(attr.ServiceName))
                        {
                            serviceCollection.RegisterScoped(attr.Type, type);
                        }
                        else
                        {
                            serviceCollection.RegisterScoped(attr.Type, type);
                        }

                        break;
                    case ServiceLifetime.Singleton:
                        if (string.IsNullOrEmpty(attr.ServiceName))
                        {
                            serviceCollection.RegisterSingleton(attr.Type, type);
                        }
                        else
                        {
                            serviceCollection.RegisterSingleton(attr.Type, type, attr.ServiceName);
                        }

                        break;
                    case ServiceLifetime.Transient:
                        if (string.IsNullOrEmpty(attr.ServiceName))
                        {
                            serviceCollection.Register(attr.Type, type);
                        }
                        else
                        {
                            serviceCollection.Register(attr.Type, type, attr.ServiceName);
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