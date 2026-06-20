using System;
using System.Threading;

namespace Prism.Mvvm;

public sealed class ViewModelLocationScope : IDisposable
{
    private int _disposed;

    internal ViewModelLocationScope(Guid id, Guid? parentId, string? moduleName)
    {
        Id = id;
        ParentId = parentId;
        Context = new ViewModelLocationContext(null, id, moduleName);
    }

    public Guid Id { get; }

    public Guid? ParentId { get; }

    public ViewModelLocationContext Context { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            ViewModelLocationProvider.ReleaseScopedViewModelFactory(Id);
        }
    }
}