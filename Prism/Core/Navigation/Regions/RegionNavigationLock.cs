using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prism.Navigation.Regions;

internal sealed class RegionNavigationLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _syncRoot = new();
    private readonly Action<RegionNavigationLock> _disposedCallback;
    private bool _removed;
    private bool _disposed;
    private int _rentCount;

    public RegionNavigationLock(Action<RegionNavigationLock> disposedCallback)
    {
        _disposedCallback = disposedCallback;
    }

    public bool TryRent()
    {
        lock (_syncRoot)
        {
            if (_removed || _disposed)
            {
                return false;
            }

            _rentCount++;
            return true;
        }
    }

    public Task<bool> WaitAsync(TimeSpan timeout)
    {
        return _semaphore.WaitAsync(timeout);
    }

    public void ReleaseRent(bool releaseSemaphore)
    {
        if (releaseSemaphore)
        {
            _semaphore.Release();
        }

        var shouldDispose = false;
        lock (_syncRoot)
        {
            _rentCount--;
            shouldDispose = ShouldDispose();
        }

        DisposeIfReady(shouldDispose);
    }

    public void MarkRemoved()
    {
        var shouldDispose = false;
        lock (_syncRoot)
        {
            _removed = true;
            shouldDispose = ShouldDispose();
        }

        DisposeIfReady(shouldDispose);
    }

    private bool ShouldDispose()
    {
        if (_disposed || !_removed || _rentCount > 0)
        {
            return false;
        }

        _disposed = true;
        return true;
    }

    private void DisposeIfReady(bool shouldDispose)
    {
        if (!shouldDispose)
        {
            return;
        }

        _semaphore.Dispose();
        _disposedCallback(this);
    }
}
