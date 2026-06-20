using System;

namespace Prism.Events;

public sealed class SubscriptionToken : IDisposable
{
    private Action? _unsubscribe;
    private readonly object _syncRoot = new();

    internal SubscriptionToken()
    {
    }

    internal SubscriptionToken(Action unsubscribe)
    {
        _unsubscribe = unsubscribe;
    }

    internal void SetUnsubscribe(Action unsubscribe)
    {
        lock (_syncRoot)
        {
            _unsubscribe = unsubscribe;
        }
    }

    public void Dispose()
    {
        Action? unsubscribe;
        lock (_syncRoot)
        {
            unsubscribe = _unsubscribe;
            _unsubscribe = null;
        }

        unsubscribe?.Invoke();
    }
}