using System.Collections.Generic;

namespace Prism.Events;

public sealed class EventSubscriptionScope : IEventSubscriptionScope
{
    private readonly List<SubscriptionToken> _tokens = new();
    private readonly object _syncRoot = new();
    private bool _disposed;

    public void Add(SubscriptionToken token)
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                token.Dispose();
                return;
            }

            _tokens.Add(token);
        }
    }

    public void Dispose()
    {
        List<SubscriptionToken> tokens;
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            tokens = new List<SubscriptionToken>(_tokens);
            _tokens.Clear();
        }

        foreach (var token in tokens)
        {
            token.Dispose();
        }
    }
}