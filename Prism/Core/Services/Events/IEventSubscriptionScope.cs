using System;

namespace Prism.Events;

public interface IEventSubscriptionScope : IDisposable
{
    void Add(SubscriptionToken token);
}