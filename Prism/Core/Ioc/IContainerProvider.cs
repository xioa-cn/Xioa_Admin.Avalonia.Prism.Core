using System;

namespace Prism.Ioc;

public interface IContainerProvider
{
    object Resolve(Type type);

    object Resolve(Type type, string? name);

    T Resolve<T>();

    T Resolve<T>(string name);

    bool IsRegistered(Type type);

    bool IsRegistered(Type type, string name);

    IContainerProvider CreateScope();

    IContainerProvider CreateScope(string? moduleName);
}