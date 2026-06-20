using System;

namespace Prism.Ioc;

public static class ContainerLocator
{
    private static IContainerExtension? _current;

    public static IContainerExtension Container =>
        _current ?? throw new InvalidOperationException("The container has not been initialized.");

    public static void SetContainerExtension(IContainerExtension containerExtension)
    {
        _current = containerExtension ?? throw new ArgumentNullException(nameof(containerExtension));
    }
}
