using System;
using Prism.Events;

namespace Ava.Xioa.Common;

/// <summary>
/// 可绑定的视图模型，提供事件聚合功能
/// </summary>
public abstract class EventEnabledViewModelObject : ReactiveObject
{
    public readonly IEventAggregator? EventAggregator;
    
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public EventEnabledViewModelObject()
    {
    }
    
    /// <summary>
    /// 带事件聚合器的构造函数
    /// </summary>
    public EventEnabledViewModelObject(IEventAggregator? aggregator)
    {
        EventAggregator = aggregator;
    }
    
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <exception cref="InvalidOperationException">当EventAggregator为空时抛出</exception>
    protected void PublishEvent<TEvent, TPayload>(TPayload payload) where TEvent : PubSubEvent<TPayload>, new()
    {
        ArgumentNullException.ThrowIfNull(EventAggregator, nameof(EventAggregator));
            
        var eventInstance = EventAggregator.GetEvent<TEvent>();
        ArgumentNullException.ThrowIfNull(eventInstance, nameof(eventInstance));
            
        eventInstance.Publish(payload);
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <exception cref="ArgumentNullException">当EventAggregator或action为空时抛出</exception>
    protected SubscriptionToken SubscribeEvent<TEvent, TPayload>(Action<TPayload> action, 
        ThreadOption threadOption = ThreadOption.PublisherThread,
        bool keepSubscriberReferenceAlive = false,
        Predicate<TPayload>? filter = null) where TEvent : PubSubEvent<TPayload>, new()
    {
        ArgumentNullException.ThrowIfNull(EventAggregator, nameof(EventAggregator));
        ArgumentNullException.ThrowIfNull(action, nameof(action));
            
        var eventInstance = EventAggregator.GetEvent<TEvent>();
        ArgumentNullException.ThrowIfNull(eventInstance, nameof(eventInstance));
            
        return eventInstance.Subscribe(action, threadOption, keepSubscriberReferenceAlive, filter);
    }
    
    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <exception cref="ArgumentNullException">当EventAggregator或token为空时抛出</exception>
    protected void UnsubscribeEvent<TEvent, TPayload>(SubscriptionToken token) where TEvent : PubSubEvent<TPayload>, new()
    {
        ArgumentNullException.ThrowIfNull(EventAggregator, nameof(EventAggregator));
        ArgumentNullException.ThrowIfNull(token, nameof(token));
            
        var eventInstance = EventAggregator.GetEvent<TEvent>();
        ArgumentNullException.ThrowIfNull(eventInstance, nameof(eventInstance));
            
        eventInstance.Unsubscribe(token);
    }
}

