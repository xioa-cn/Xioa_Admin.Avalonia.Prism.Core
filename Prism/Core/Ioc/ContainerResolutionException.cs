using System;

namespace Prism.Ioc;

public sealed class ContainerResolutionException : InvalidOperationException
{
    public ContainerResolutionException(Type serviceType, string? name, string message, Exception? innerException)
        : base(message, innerException)
    {
        ServiceType = serviceType;
        Name = name;
    }

    public Type ServiceType { get; }

    public string? Name { get; }
}