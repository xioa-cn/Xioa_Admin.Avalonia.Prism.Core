using System;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AutoDbContextAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped) : Attribute
{
    /// <summary>
    /// 注入类型
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = lifetime;
}