using System.ComponentModel;

namespace Prism;

public sealed class PropertyChangingCancelEventArgs : PropertyChangingEventArgs
{
    public PropertyChangingCancelEventArgs(string? propertyName)
        : base(propertyName)
    {
    }

    public bool Cancel { get; set; }
}