using System;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Constructor)]
public sealed class ConstructorAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ServiceAttribute<T> : Attribute where T : class
{
    public ServiceLifetime ServiceLifetime { get; }
    public Type ServiceType { get; }
    public string ServiceName { get; }

    public ServiceAttribute()
        : this(string.Empty, ServiceLifetime.Singleton)
    {
    }

    public ServiceAttribute(ServiceLifetime serviceLifetime)
        : this(string.Empty, serviceLifetime)
    {
    }

    public ServiceAttribute(string serviceName)
        : this(serviceName, ServiceLifetime.Singleton)
    {
    }

    public ServiceAttribute(string serviceName, ServiceLifetime serviceLifetime)
    {
        ServiceName = serviceName;
        ServiceLifetime = serviceLifetime;
        ServiceType = typeof(T);
    }
}
