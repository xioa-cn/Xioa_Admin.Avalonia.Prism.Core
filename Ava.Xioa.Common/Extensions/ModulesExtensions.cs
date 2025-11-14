using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Utils;
using Prism.Modularity;

namespace Ava.Xioa.Common.Extensions;

public static class ModulesExtensions
{
    public static IModuleCatalog AddAutoModule(
        this IModuleCatalog catalog)
    {
        var compilationLibrary = DependencyCompilation.GetCompilationLibrary();
        if (compilationLibrary == null) return catalog;
        
        foreach (var compilation in compilationLibrary)
        {
            var types = AssemblyLoadContext.Default
                .LoadFromAssemblyName(new AssemblyName(compilation.Name))
                .GetTypes().Where(a =>
                    a.GetCustomAttribute<AutoModuleAttribute>() != null)
                .ToList();
            if (types.Count <= 0) continue;
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<AutoModuleAttribute>();
                if (attr == null) continue;
                
                catalog.AddModule(type);
            }
        }

        
        return catalog;
    }
}