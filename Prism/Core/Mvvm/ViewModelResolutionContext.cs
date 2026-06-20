using System;

namespace Prism.Mvvm;

public sealed class ViewModelResolutionContext
{
    public ViewModelResolutionContext(Type viewType, string? routeName, string? moduleName)
    {
        ViewType = viewType;
        RouteName = routeName;
        ModuleName = moduleName;
    }

    public Type ViewType { get; }

    public string? RouteName { get; }

    public string? ModuleName { get; }
}