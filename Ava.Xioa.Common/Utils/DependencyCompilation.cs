using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Ava.Xioa.Common.Utils;

public static class DependencyCompilation
{
    public static IEnumerable<CompilationLibrary>? GetCompilationLibrary()
    {
        Assembly entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        DependencyContext context = DependencyContext.Load(entryAssembly);

        if (context == null)
            return Enumerable.Empty<CompilationLibrary>();

        return context.CompileLibraries
            .Where(x => !x.Serviceable && x.Type == "project");
        
        // return DependencyContext
        //     .Default?
        //     .CompileLibraries
        //     .Where(x => !x.Serviceable && x.Type != "package" && x.Type == "project");
    }
}