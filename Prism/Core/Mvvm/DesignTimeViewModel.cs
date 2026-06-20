using System;

namespace Prism.Mvvm;

public sealed class DesignTimeViewModel
{
    internal DesignTimeViewModel(Type viewType, Type? requestedViewModelType)
    {
        ViewType = viewType;
        RequestedViewModelType = requestedViewModelType;
    }

    public Type ViewType { get; }

    public Type? RequestedViewModelType { get; }
}