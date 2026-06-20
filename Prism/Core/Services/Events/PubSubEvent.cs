namespace Prism.Events;

public class PubSubEvent : EventBase
{
    public SubscriptionToken Subscribe(Action action)
    {
        return InternalSubscribe(action, invokeAction: InvokeAction);
    }

    public SubscriptionToken Subscribe(Action action, ThreadOption threadOption)
    {
        return InternalSubscribe(action, threadOption, invokeAction: InvokeAction);
    }

    public SubscriptionToken Subscribe(Action action, ThreadOption threadOption, bool keepSubscriberReferenceAlive)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, invokeAction: InvokeAction);
    }

    public SubscriptionToken Subscribe(Action action, ThreadOption threadOption, bool keepSubscriberReferenceAlive, Func<bool>? filter)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, filter, invokeAction: InvokeAction, invokeFilter: InvokeFilter);
    }

    public SubscriptionToken Subscribe(Func<Task> action)
    {
        return InternalSubscribe(action, invokeAction: InvokeAsyncAction);
    }

    public SubscriptionToken Subscribe(Func<Task> action, ThreadOption threadOption)
    {
        return InternalSubscribe(action, threadOption, invokeAction: InvokeAsyncAction);
    }

    public SubscriptionToken Subscribe(Func<Task> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, invokeAction: InvokeAsyncAction);
    }

    public SubscriptionToken Subscribe(Func<Task> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive, Func<bool>? filter)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, filter, invokeAction: InvokeAsyncAction, invokeFilter: InvokeFilter);
    }

    public SubscriptionToken SubscribeOnce(Action action, ThreadOption threadOption = ThreadOption.PublisherThread, bool keepSubscriberReferenceAlive = false, Func<bool>? filter = null, string? name = null, int priority = 0)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, filter, name, priority, once: true, invokeAction: InvokeAction, invokeFilter: InvokeFilter);
    }

    public SubscriptionToken SubscribeOnce(Func<Task> action, ThreadOption threadOption = ThreadOption.PublisherThread, bool keepSubscriberReferenceAlive = false, Func<bool>? filter = null, string? name = null, int priority = 0)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, filter, name, priority, once: true, invokeAction: InvokeAsyncAction, invokeFilter: InvokeFilter);
    }

    public SubscriptionToken Subscribe(Action action, string name, int priority = 0, ThreadOption threadOption = ThreadOption.PublisherThread, bool keepSubscriberReferenceAlive = false, Func<bool>? filter = null)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, filter, name, priority, invokeAction: InvokeAction, invokeFilter: InvokeFilter);
    }

    public void Publish()
    {
        InternalPublish(null);
    }

    public void Unsubscribe(SubscriptionToken token)
    {
        InternalUnsubscribe(token);
    }

    private static Task? InvokeAction(Delegate action, object? _)
    {
        ((Action)action)();
        return null;
    }

    private static Task? InvokeAsyncAction(Delegate action, object? _)
    {
        return ((Func<Task>)action)();
    }

    private static bool InvokeFilter(Delegate filter, object? _)
    {
        return ((Func<bool>)filter)();
    }
}