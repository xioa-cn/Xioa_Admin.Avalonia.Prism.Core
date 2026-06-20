namespace Prism.Events;

public class PubSubEvent<TPayload> : EventBase
{
    public SubscriptionToken Subscribe(Action<TPayload> action)
    {
        return InternalSubscribe(action, invokeAction: InvokeAction);
    }

    public SubscriptionToken Subscribe(Action<TPayload> action, ThreadOption threadOption)
    {
        return InternalSubscribe(action, threadOption, invokeAction: InvokeAction);
    }

    public SubscriptionToken Subscribe(Action<TPayload> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, invokeAction: InvokeAction);
    }

    public SubscriptionToken Subscribe(Action<TPayload> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive, Predicate<TPayload>? filter)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, filter, invokeAction: InvokeAction, invokeFilter: InvokeFilter);
    }

    public SubscriptionToken Subscribe(Func<TPayload, Task> action)
    {
        return InternalSubscribe(action, invokeAction: InvokeAsyncAction);
    }

    public SubscriptionToken Subscribe(Func<TPayload, Task> action, ThreadOption threadOption)
    {
        return InternalSubscribe(action, threadOption, invokeAction: InvokeAsyncAction);
    }

    public SubscriptionToken Subscribe(Func<TPayload, Task> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, invokeAction: InvokeAsyncAction);
    }

    public SubscriptionToken Subscribe(Func<TPayload, Task> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive, Predicate<TPayload>? filter)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, filter, invokeAction: InvokeAsyncAction, invokeFilter: InvokeFilter);
    }

    public SubscriptionToken SubscribeOnce(Action<TPayload> action, ThreadOption threadOption = ThreadOption.PublisherThread, bool keepSubscriberReferenceAlive = false, Predicate<TPayload>? filter = null, string? name = null, int priority = 0)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, filter, name, priority, once: true, invokeAction: InvokeAction, invokeFilter: InvokeFilter);
    }

    public SubscriptionToken SubscribeOnce(Func<TPayload, Task> action, ThreadOption threadOption = ThreadOption.PublisherThread, bool keepSubscriberReferenceAlive = false, Predicate<TPayload>? filter = null, string? name = null, int priority = 0)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, filter, name, priority, once: true, invokeAction: InvokeAsyncAction, invokeFilter: InvokeFilter);
    }

    public SubscriptionToken Subscribe(Action<TPayload> action, string name, int priority = 0, ThreadOption threadOption = ThreadOption.PublisherThread, bool keepSubscriberReferenceAlive = false, Predicate<TPayload>? filter = null)
    {
        return InternalSubscribe(action, threadOption, keepSubscriberReferenceAlive, filter, name, priority, invokeAction: InvokeAction, invokeFilter: InvokeFilter);
    }

    public void Publish(TPayload payload)
    {
        InternalPublish(payload);
    }

    public void Unsubscribe(SubscriptionToken token)
    {
        InternalUnsubscribe(token);
    }

    private static Task? InvokeAction(Delegate action, object? payload)
    {
        ((Action<TPayload>)action)((TPayload)payload!);
        return null;
    }

    private static Task? InvokeAsyncAction(Delegate action, object? payload)
    {
        return ((Func<TPayload, Task>)action)((TPayload)payload!);
    }

    private static bool InvokeFilter(Delegate filter, object? payload)
    {
        return ((Predicate<TPayload>)filter)((TPayload)payload!);
    }
}