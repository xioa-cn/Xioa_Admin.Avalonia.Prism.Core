using System;
using Ava.Xioa.Common.Models;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RegisterForNavigationAttribute : Attribute
{
    public RegisterForNavigationAttribute(string navigationName, string region,
        ProgrammingVersion version = ProgrammingVersion.EnabledStandby)
    {
        NavigationName = navigationName;
        Region = region;
        Version = version;
    }

    public ProgrammingVersion Version { get; set; }
    public string? NavigationName { get; set; }

    public string Region { get; set; }
}