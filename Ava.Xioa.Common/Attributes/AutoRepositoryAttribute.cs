using System;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AutoRepositoryAttribute(Type interfaceType, ServiceLifetime lifetime): Attribute
{
    public Type Type { get; set; } = interfaceType;

    /// <summary>
    /// 注入类型
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = lifetime;
}