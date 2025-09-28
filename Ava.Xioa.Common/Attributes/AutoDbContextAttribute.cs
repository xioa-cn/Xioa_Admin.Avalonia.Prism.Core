using System;
using Ava.Xioa.Common.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AutoDbContextAttribute(
    ServiceLifetime lifetime = ServiceLifetime.Scoped,
    ProgrammingVersion version = ProgrammingVersion.EnabledStandby) : Attribute
{
    /// <summary>
    /// 注入类型
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = lifetime;

    public ProgrammingVersion Version { get; set; } = version;
}