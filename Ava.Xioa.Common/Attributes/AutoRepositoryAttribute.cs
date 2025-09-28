using System;
using Ava.Xioa.Common.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AutoRepositoryAttribute(
    Type interfaceType,
    ServiceLifetime lifetime,
    ProgrammingVersion version = ProgrammingVersion.EnabledStandby) : Attribute
{
    public Type Type { get; set; } = interfaceType;
    public ProgrammingVersion Version { get; set; } = version;

    /// <summary>
    /// 注入类型
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = lifetime;
}