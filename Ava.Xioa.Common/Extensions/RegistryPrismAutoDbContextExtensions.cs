using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Utils;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

namespace Ava.Xioa.Common.Extensions;

public static class RegistryPrismAutoDbContextExtensions
{
    public static IContainerRegistry AddPrismAutoDbContext(this IContainerRegistry serviceCollection)
    {
        var compilationLibrary = DependencyCompilation.GetCompilationLibrary();
        if (compilationLibrary == null) return serviceCollection;
        foreach (var compilation in compilationLibrary)
        {
            var types = AssemblyLoadContext.Default
                .LoadFromAssemblyName(new AssemblyName(compilation.Name))
                .GetTypes().Where(a =>
                    a.GetCustomAttribute<AutoDbContextAttribute>() != null)
                .ToList();
            if (types.Count <= 0) continue;
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<AutoDbContextAttribute>();
                if (attr == null) continue;

                if (attr.Version == ProgrammingVersion.Obsolete)
                {
                    continue;
                }
                
                switch (attr.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        serviceCollection.RegisterScoped(type, type);
                        break;
                    case ServiceLifetime.Singleton:
                        serviceCollection.RegisterSingleton(type, type);
                        break;
                    case ServiceLifetime.Transient:
                        serviceCollection.Register(type, type);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        return serviceCollection;
    }
}