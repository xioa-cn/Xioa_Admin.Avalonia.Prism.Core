using System;
using Ava.Xioa.Common.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PrismServiceAttribute : Attribute
{
    /// <summary>
    /// 服务生命周期类型
    /// </summary>
    public ServiceLifetime Lifetime { get; set; }

    public ProgrammingVersion Version { get; set; }

    /// <summary>
    /// 服务注册名称
    /// </summary>
    public string? ServiceName { get; set; }

    public Type Type { get; set; }

    public PrismServiceAttribute(Type type, ServiceLifetime lifetime = ServiceLifetime.Singleton,
        string serviceName = "", ProgrammingVersion version = ProgrammingVersion.EnabledStandby)
    {
        Lifetime = lifetime;
        Type = type;
        Version = version;
        ServiceName = serviceName;
    }
}