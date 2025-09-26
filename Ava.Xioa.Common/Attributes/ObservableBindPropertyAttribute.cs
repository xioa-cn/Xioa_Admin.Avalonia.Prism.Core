using System;

namespace Ava.Xioa.Common.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ObservableBindPropertyAttribute : Attribute
{
    public string? PropertyName { get; }
    public bool IncludeSetter { get; set; } = true;
    public bool IncludeOnChangedMethod { get; set; }
    public bool IncludeOnChangingMethod { get; set; }

    public ObservableBindPropertyAttribute() { }

    public ObservableBindPropertyAttribute(string propertyName)
    {
        PropertyName = propertyName;
    }
}