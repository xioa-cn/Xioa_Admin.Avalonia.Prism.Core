using System;

namespace Prism.Mvvm;

public sealed class ViewModelLocationContext
{
    internal ViewModelLocationContext(Func<object?, Type, object>? scopedFactory, Guid? scopeId, string? moduleName)
    {
        ScopedFactory = scopedFactory;
        ScopeId = scopeId;
        ModuleName = moduleName;
    }

    internal Func<object?, Type, object>? ScopedFactory { get; }

    internal Guid? ScopeId { get; }

    internal string? ModuleName { get; }
}