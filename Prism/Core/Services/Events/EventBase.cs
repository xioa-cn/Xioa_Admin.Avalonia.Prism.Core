using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prism.Events;

public abstract class EventBase
{
    private static IEventBackgroundDispatcher _backgroundDispatcher = new ThreadPoolEventBackgroundDispatcher();
    private readonly List<EventSubscription> _subscriptions = new();
    private readonly object _syncRoot = new();

    public event EventHandler<EventSubscriptionExceptionEventArgs>? SubscriptionException;

    public event EventHandler<EventSubscriptionExecutionEventArgs>? SlowSubscription;

    public TimeSpan? SubscriptionTimeout { get; set; }

    public TimeSpan? SlowSubscriptionThreshold { get; set; }

    public static IEventBackgroundDispatcher BackgroundDispatcher
    {
        get => _backgroundDispatcher;
        set => _backgroundDispatcher = value ?? throw new ArgumentNullException(nameof(value));
    }

    protected SubscriptionToken InternalSubscribe(
        Delegate action,
        ThreadOption threadOption = ThreadOption.PublisherThread,
        bool keepSubscriberReferenceAlive = false,
        Delegate? filter = null,
        string? name = null,
        int priority = 0,
        bool once = false,
        Func<Delegate, object?, Task?>? invokeAction = null,
        Func<Delegate, object?, bool>? invokeFilter = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        var token = new SubscriptionToken();
        var subscription = new EventSubscription(
            token,
            action,
            filter,
            threadOption,
            keepSubscriberReferenceAlive,
            name,
            priority,
            once,
            invokeAction,
            invokeFilter);
        token.SetUnsubscribe(() => InternalUnsubscribe(token));

        lock (_syncRoot)
        {
            _subscriptions.Add(subscription);
            _subscriptions.Sort(static (left, right) => right.Priority.CompareTo(left.Priority));
        }

        return token;
    }

    protected void InternalUnsubscribe(SubscriptionToken token)
    {
        lock (_syncRoot)
        {
            _subscriptions.RemoveAll(subscription => ReferenceEquals(subscription.Token, token));
        }
    }

    protected void InternalUnsubscribe(string name)
    {
        lock (_syncRoot)
        {
            _subscriptions.RemoveAll(subscription => string.Equals(subscription.Name, name, StringComparison.Ordinal));
        }
    }

    public void Unsubscribe(string name)
    {
        InternalUnsubscribe(name);
    }

    protected void InternalPublish(object? payload)
    {
        List<EventSubscription> subscribers;
        lock (_syncRoot)
        {
            subscribers = new List<EventSubscription>(_subscriptions);
        }

        var staleSubscriptions = new List<EventSubscription>();
        foreach (var subscriber in subscribers)
        {
            if (!subscriber.TryInvoke(payload, OnSubscriptionException, OnSlowSubscription, SubscriptionTimeout, SlowSubscriptionThreshold))
            {
                staleSubscriptions.Add(subscriber);
                continue;
            }

            if (subscriber.Once)
            {
                staleSubscriptions.Add(subscriber);
            }
        }

        if (staleSubscriptions.Count == 0)
        {
            return;
        }

        lock (_syncRoot)
        {
            foreach (var staleSubscription in staleSubscriptions)
            {
                _subscriptions.Remove(staleSubscription);
            }
        }
    }

    private void OnSubscriptionException(SubscriptionToken token, Exception exception)
    {
        SubscriptionException?.Invoke(this, new EventSubscriptionExceptionEventArgs(this, token, exception));
    }

    private void OnSlowSubscription(SubscriptionToken token, TimeSpan elapsed)
    {
        SlowSubscription?.Invoke(this, new EventSubscriptionExecutionEventArgs(this, token, elapsed));
    }
}