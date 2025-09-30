using System;
using Ava.Xioa.Common.Models;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PrismRegisterForNavigationAttribute : Attribute
{
    public PrismRegisterForNavigationAttribute(string navigationName, string region,
        ProgrammingVersion version = ProgrammingVersion.EnabledStandby, int zIndex = -1)
    {
        NavigationName = navigationName;
        Region = region;
        ZIndex = zIndex;
        Version = version;
    }

    public int ZIndex { get; set; }

    public ProgrammingVersion Version { get; set; }
    public string? NavigationName { get; set; }

    public string Region { get; set; }
}