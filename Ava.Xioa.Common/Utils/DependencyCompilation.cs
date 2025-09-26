using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace Ava.Xioa.Common.Utils;

public static class DependencyCompilation
{
    public static IEnumerable<CompilationLibrary>? GetCompilationLibrary()
    {
        return DependencyContext
            .Default?
            .CompileLibraries
            .Where(x => !x.Serviceable && x.Type != "package" && x.Type == "project");
    }
}