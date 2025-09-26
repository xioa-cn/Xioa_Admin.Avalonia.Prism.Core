using System;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PrismVmAttribute : Attribute
{
    /// <summary>
    /// 服务生命周期类型
    /// </summary>
    public ServiceLifetime Lifetime { get; set; }

    /// <summary>
    /// 服务注册名称
    /// </summary>
    public string? ServiceName { get; set; }

    public Type Type { get; set; }

    public PrismVmAttribute(Type type, ServiceLifetime lifetime = ServiceLifetime.Singleton,
        string serviceName = "")
    {
        Lifetime = lifetime;
        Type = type;
        ServiceName = serviceName;
    }
}