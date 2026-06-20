namespace Prism.Events;

public sealed class EventSubscriptionExecutionEventArgs : EventArgs
{
    public EventSubscriptionExecutionEventArgs(EventBase eventBase, SubscriptionToken token, TimeSpan elapsed)
    {
        EventBase = eventBase;
        Token = token;
        Elapsed = elapsed;
    }

    public EventBase EventBase { get; }

    public SubscriptionToken Token { get; }

    public TimeSpan Elapsed { get; }
}