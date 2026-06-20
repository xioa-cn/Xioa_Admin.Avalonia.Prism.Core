namespace Prism.Mvvm;

public sealed class ViewModelLocationScopeChangedEventArgs : EventArgs
{
    public ViewModelLocationScopeChangedEventArgs(Guid scopeId, Guid? parentScopeId, string? moduleName, int activeUseCount = 0)
    {
        ScopeId = scopeId;
        ParentScopeId = parentScopeId;
        ModuleName = moduleName;
        ActiveUseCount = activeUseCount;
    }

    public Guid ScopeId { get; }

    public Guid? ParentScopeId { get; }

    public string? ModuleName { get; }

    public int ActiveUseCount { get; }
}