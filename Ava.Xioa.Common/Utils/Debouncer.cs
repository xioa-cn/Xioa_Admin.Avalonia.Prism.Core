using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ava.Xioa.Common.Utils;

/// <summary>
/// C# 防抖工具类
/// </summary>
public class Debouncer : IDisposable
{
    // 用于取消前一次的延迟任务
    private CancellationTokenSource _cts;

    // 防抖延迟时间（毫秒）
    private readonly int _delayMilliseconds;

    // 是否已释放资源
    private bool _disposed;

    /// <summary>
    /// 初始化防抖器
    /// </summary>
    /// <param name="delayMilliseconds">防抖延迟时间（毫秒），建议 100-500ms</param>
    public Debouncer(int delayMilliseconds)
    {
        if (delayMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "延迟时间不能为负数");

        _delayMilliseconds = delayMilliseconds;
    }

    /// <summary>
    /// 触发防抖（同步目标函数）
    /// </summary>
    /// <param name="action">需要防抖的同步函数</param>
    public void Debounce(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (_disposed) throw new ObjectDisposedException(nameof(Debouncer));

        // 取消前一次的延迟任务
        _cts?.Cancel();
        _cts?.Dispose();

        // 创建新的取消令牌源
        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;

        // 延迟执行目标函数
        _ = Task.Delay(_delayMilliseconds, cancellationToken)
            .ContinueWith(t =>
            {
                if (!t.IsCanceled) // 确保任务未被取消
                    action();
            }, cancellationToken);
    }

    /// <summary>
    /// 触发防抖（带参数的同步目标函数）
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="action">带参数的同步函数</param>
    /// <param name="param">传递给函数的参数</param>
    public void Debounce<T>(Action<T> action, T param)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        Debounce(() => action(param)); // 包装为无参函数
    }

    /// <summary>
    /// 触发防抖（异步目标函数）
    /// </summary>
    /// <param name="func">需要防抖的异步函数</param>
    public void DebounceAsync(Func<Task> func)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        if (_disposed) throw new ObjectDisposedException(nameof(Debouncer));

        // 取消前一次的延迟任务
        _cts?.Cancel();
        _cts?.Dispose();

        // 创建新的取消令牌源
        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;

        // 延迟执行异步目标函数
        _ = Task.Delay(_delayMilliseconds, cancellationToken)
            .ContinueWith(async t =>
            {
                if (!t.IsCanceled)
                    await func(); // 等待异步函数完成
            }, cancellationToken);
    }

    /// <summary>
    /// 手动取消当前防抖任务
    /// </summary>
    public void Cancel()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// 释放资源（避免内存泄漏）
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // 释放托管资源（取消令牌源）
            _cts?.Cancel();
            _cts?.Dispose();
        }

        _disposed = true;
    }

    ~Debouncer()
    {
        Dispose(false);
    }
}