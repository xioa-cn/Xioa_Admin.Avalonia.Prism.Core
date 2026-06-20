using System;
using System.Collections.Generic;

namespace Prism.Mvvm;

public sealed class ViewModelLocationScopeNode
{
    public ViewModelLocationScopeNode(Guid scopeId, Guid? parentScopeId, string? moduleName, int activeUseCount, IReadOnlyList<ViewModelLocationScopeNode> children)
    {
        ScopeId = scopeId;
        ParentScopeId = parentScopeId;
        ModuleName = moduleName;
        ActiveUseCount = activeUseCount;
        Children = children;
    }

    public Guid ScopeId { get; }

    public Guid? ParentScopeId { get; }

    public string? ModuleName { get; }

    public int ActiveUseCount { get; }

    public IReadOnlyList<ViewModelLocationScopeNode> Children { get; }
}