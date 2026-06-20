namespace Prism.Events;

public sealed class EventSubscriptionExceptionEventArgs : EventArgs
{
    public EventSubscriptionExceptionEventArgs(EventBase eventBase, SubscriptionToken token, Exception exception)
    {
        EventBase = eventBase;
        Token = token;
        Exception = exception;
    }

    public EventBase EventBase { get; }

    public SubscriptionToken Token { get; }

    public Exception Exception { get; }
}
