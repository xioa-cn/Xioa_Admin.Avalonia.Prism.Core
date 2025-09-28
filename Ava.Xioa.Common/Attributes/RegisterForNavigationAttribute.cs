using System;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RegisterForNavigationAttribute : Attribute
{
    public RegisterForNavigationAttribute(string navigationName, string region)
    {
        NavigationName = navigationName;
        Region = region;
    }
    
    public string? NavigationName { get; set; }
    
    public string Region { get; set; }
}