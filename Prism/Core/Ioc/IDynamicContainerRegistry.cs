using System;

namespace Prism.Ioc;

public interface IDynamicContainerRegistry
{
    IDisposable BeginModuleDynamic(string moduleName);

    IDisposable ResetModuleDynamicContext();

    void ClearModuleDynamic(string moduleName);

    bool Unregister(Type type, string? name = null);

    IContainerRegistry CreateScopeRegistry(string? moduleName = null);
}

public interface IDynamicContainerPartition
{
    bool Unregister(Type type, string? name = null);
}
