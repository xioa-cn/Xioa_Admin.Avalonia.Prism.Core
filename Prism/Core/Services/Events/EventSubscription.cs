using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Threading;

namespace Prism.Events;

internal sealed class EventSubscription
{
    private static readonly Dictionary<DelegateInvokerKey, Func<Delegate, object?, Task?>> ActionInvokerCache = new();
    private static readonly Dictionary<DelegateInvokerKey, Func<Delegate, object?, bool>> FilterInvokerCache = new();
    private static readonly object InvokerCacheSyncRoot = new();
    private readonly DelegateReference _actionReference;
    private readonly DelegateReference? _filterReference;
    private readonly ThreadOption _threadOption;
    private readonly Func<Delegate, object?, Task?> _invokeAction;
    private readonly Func<Delegate, object?, bool> _invokeFilter;

    public EventSubscription(
        SubscriptionToken token,
        Delegate action,
        Delegate? filter,
        ThreadOption threadOption,
        bool keepSubscriberReferenceAlive,
        string? name,
        int priority,
        bool once,
        Func<Delegate, object?, Task?>? invokeAction,
        Func<Delegate, object?, bool>? invokeFilter)
    {
        Token = token;
        _actionReference = new DelegateReference(action, keepSubscriberReferenceAlive);
        _filterReference = filter is null ? null : new DelegateReference(filter, keepSubscriberReferenceAlive);
        _threadOption = threadOption;
        Name = name;
        Priority = priority;
        Once = once;
        _invokeAction = invokeAction ?? InvokeActionByReflection;
        _invokeFilter = invokeFilter ?? CreateFilterInvoker(filter);
    }

    public SubscriptionToken Token { get; }

    public string? Name { get; }

    public int Priority { get; }

    public bool Once { get; }

    public bool TryInvoke(
        object? payload,
        Action<SubscriptionToken, Exception> exceptionHandler,
        Action<SubscriptionToken, TimeSpan> slowSubscriptionHandler,
        TimeSpan? timeout,
        TimeSpan? slowSubscriptionThreshold)
    {
        var action = _actionReference.Target;
        if (action is null)
        {
            return false;
        }

        var filter = _filterReference?.Target;
        if (_filterReference is not null && filter is null)
        {
            return false;
        }

        if (filter is not null && !_invokeFilter(filter, payload))
        {
            return true;
        }

        Dispatch(action, payload, exceptionHandler, slowSubscriptionHandler, timeout, slowSubscriptionThreshold);
        return true;
    }

    private void Dispatch(
        Delegate action,
        object? payload,
        Action<SubscriptionToken, Exception> exceptionHandler,
        Action<SubscriptionToken, TimeSpan> slowSubscriptionHandler,
        TimeSpan? timeout,
        TimeSpan? slowSubscriptionThreshold)
    {
        switch (_threadOption)
        {
            case ThreadOption.BackgroundThread:
                EventBase.BackgroundDispatcher.Dispatch(() => SafeInvokeAction(action, payload, exceptionHandler, slowSubscriptionHandler, timeout, slowSubscriptionThreshold));
                break;
            case ThreadOption.UIThreadSend:
                DispatchOnUiThread(action, payload, exceptionHandler, slowSubscriptionHandler, timeout, slowSubscriptionThreshold, waitForCompletion: true);
                break;
            case ThreadOption.UIThread:
                DispatchOnUiThread(action, payload, exceptionHandler, slowSubscriptionHandler, timeout, slowSubscriptionThreshold, waitForCompletion: false);
                break;
            default:
                SafeInvokeAction(action, payload, exceptionHandler, slowSubscriptionHandler, timeout, slowSubscriptionThreshold);
                break;
        }
    }

    private void DispatchOnUiThread(
        Delegate action,
        object? payload,
        Action<SubscriptionToken, Exception> exceptionHandler,
        Action<SubscriptionToken, TimeSpan> slowSubscriptionHandler,
        TimeSpan? timeout,
        TimeSpan? slowSubscriptionThreshold,
        bool waitForCompletion)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            SafeInvokeAction(action, payload, exceptionHandler, slowSubscriptionHandler, timeout, slowSubscriptionThreshold);
            return;
        }

        if (waitForCompletion)
        {
            Dispatcher.UIThread.Invoke(() => SafeInvokeAction(action, payload, exceptionHandler, slowSubscriptionHandler, timeout, slowSubscriptionThreshold));
            return;
        }

        Dispatcher.UIThread.Post(() => SafeInvokeAction(action, payload, exceptionHandler, slowSubscriptionHandler, timeout, slowSubscriptionThreshold));
    }

    private static Func<Delegate, object?, bool> CreateFilterInvoker(Delegate? filter)
    {
        if (filter is null)
        {
            return static (_, _) => true;
        }

        lock (InvokerCacheSyncRoot)
        {
            var key = new DelegateInvokerKey(filter.GetType(), filter.Method);
            if (!FilterInvokerCache.TryGetValue(key, out var invoker))
            {
                invoker = CompileFilterInvoker(filter);
                FilterInvokerCache[key] = invoker;
            }

            return invoker;
        }
    }

    private void SafeInvokeAction(
        Delegate action,
        object? payload,
        Action<SubscriptionToken, Exception> exceptionHandler,
        Action<SubscriptionToken, TimeSpan> slowSubscriptionHandler,
        TimeSpan? timeout,
        TimeSpan? slowSubscriptionThreshold)
    {
        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            var result = _invokeAction(action, payload);
            if (result is Task task)
            {
                _ = ObserveTaskAsync(task, exceptionHandler, slowSubscriptionHandler, startedAt, timeout, slowSubscriptionThreshold);
            }
            else
            {
                ReportElapsed(startedAt, slowSubscriptionHandler, slowSubscriptionThreshold);
            }
        }
        catch (Exception ex)
        {
            exceptionHandler(Token, UnwrapInvocationException(ex));
        }
    }

    private async Task ObserveTaskAsync(
        Task task,
        Action<SubscriptionToken, Exception> exceptionHandler,
        Action<SubscriptionToken, TimeSpan> slowSubscriptionHandler,
        DateTimeOffset startedAt,
        TimeSpan? timeout,
        TimeSpan? slowSubscriptionThreshold)
    {
        try
        {
            if (timeout is null)
            {
                await task.ConfigureAwait(false);
            }
            else
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout.Value)).ConfigureAwait(false);
                if (!ReferenceEquals(completedTask, task))
                {
                    exceptionHandler(Token, new TimeoutException($"Event subscription '{Name ?? "<unnamed>"}' timed out after {timeout.Value}."));
                    return;
                }

                await task.ConfigureAwait(false);
            }

            ReportElapsed(startedAt, slowSubscriptionHandler, slowSubscriptionThreshold);
        }
        catch (Exception ex)
        {
            exceptionHandler(Token, ex);
        }
    }

    private void ReportElapsed(DateTimeOffset startedAt, Action<SubscriptionToken, TimeSpan> slowSubscriptionHandler, TimeSpan? slowSubscriptionThreshold)
    {
        if (slowSubscriptionThreshold is null)
        {
            return;
        }

        var elapsed = DateTimeOffset.UtcNow - startedAt;
        if (elapsed >= slowSubscriptionThreshold.Value)
        {
            slowSubscriptionHandler(Token, elapsed);
        }
    }

    private static Task? InvokeActionByReflection(Delegate action, object? payload)
    {
        lock (InvokerCacheSyncRoot)
        {
            var key = new DelegateInvokerKey(action.GetType(), action.Method);
            if (!ActionInvokerCache.TryGetValue(key, out var invoker))
            {
                invoker = CompileActionInvoker(action);
                ActionInvokerCache[key] = invoker;
            }

            return invoker(action, payload);
        }
    }

    private static Func<Delegate, object?, Task?> CompileActionInvoker(Delegate sample)
    {
        var delegateParameter = Expression.Parameter(typeof(Delegate), "delegate");
        var payloadParameter = Expression.Parameter(typeof(object), "payload");
        var typedDelegate = Expression.Convert(delegateParameter, sample.GetType());
        var invokeMethod = sample.GetType().GetMethod("Invoke")!;
        var invokeParameters = invokeMethod.GetParameters();
        var arguments = invokeParameters.Length == 0
            ? Array.Empty<Expression>()
            : new[] { Expression.Convert(payloadParameter, invokeParameters[0].ParameterType) };
        var invokeCall = Expression.Call(typedDelegate, invokeMethod, arguments);

        Expression body;
        if (invokeMethod.ReturnType == typeof(void))
        {
            body = Expression.Block(invokeCall, Expression.Constant(null, typeof(Task)));
        }
        else if (typeof(Task).IsAssignableFrom(invokeMethod.ReturnType))
        {
            body = Expression.Convert(invokeCall, typeof(Task));
        }
        else
        {
            body = Expression.Block(invokeCall, Expression.Constant(null, typeof(Task)));
        }

        return Expression.Lambda<Func<Delegate, object?, Task?>>(body, delegateParameter, payloadParameter).Compile();
    }

    private static Func<Delegate, object?, bool> CompileFilterInvoker(Delegate sample)
    {
        var delegateParameter = Expression.Parameter(typeof(Delegate), "delegate");
        var payloadParameter = Expression.Parameter(typeof(object), "payload");
        var typedDelegate = Expression.Convert(delegateParameter, sample.GetType());
        var invokeMethod = sample.GetType().GetMethod("Invoke")!;
        var invokeParameters = invokeMethod.GetParameters();
        var arguments = invokeParameters.Length == 0
            ? Array.Empty<Expression>()
            : new[] { Expression.Convert(payloadParameter, invokeParameters[0].ParameterType) };
        var invokeCall = Expression.Call(typedDelegate, invokeMethod, arguments);
        Expression body = invokeMethod.ReturnType == typeof(bool)
            ? invokeCall
            : Expression.Constant(true);

        return Expression.Lambda<Func<Delegate, object?, bool>>(body, delegateParameter, payloadParameter).Compile();
    }

    private static Exception UnwrapInvocationException(Exception exception)
    {
        if (exception is TargetInvocationException targetInvocationException &&
            targetInvocationException.InnerException is { } innerException)
        {
            return innerException;
        }

        return exception;
    }
}