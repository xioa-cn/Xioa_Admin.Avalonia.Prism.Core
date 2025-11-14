using System;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AutoModuleAttribute : Attribute
{
    public AutoModuleAttribute(string moduleName)
    {
        ModuleName = moduleName;
    }

    public string ModuleName { get; }
}