namespace Prism.Events;

public class EventAggregator : IEventAggregator
{
    private readonly Dictionary<Type, EventBase> _events = new();

    public TEventType GetEvent<TEventType>()
        where TEventType : EventBase, new()
    {
        lock (_events)
        {
            if (!_events.TryGetValue(typeof(TEventType), out var existing))
            {
                existing = new TEventType();
                _events[typeof(TEventType)] = existing;
            }

            return (TEventType)existing;
        }
    }
}