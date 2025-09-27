using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ava.Xioa.Common.Input;

/// <summary>
/// 将异步方法中继到其调用者。
/// </summary>
public sealed partial class AsyncRelayCommand : IAsyncRelayCommand, ICancellationAwareCommand
{
    /// <summary>
    /// 要执行的异步委托。
    /// </summary>
    private readonly Func<CancellationToken, Task> execute;

    /// <summary>
    /// 可选的同步谓词，用于确定命令是否可以执行。
    /// </summary>
    private readonly Func<bool>? canExecute;

    /// <summary>
    /// 当前正在执行的任务的取消令牌源（如果有）。
    /// </summary>
    private CancellationTokenSource? cancellationTokenSource;

    /// <summary>
    /// 当前正在执行的任务（如果有）。
    /// </summary>
    private Task? executionTask;

    /// <summary>
    /// 当命令的可执行状态可能已更改时发生。
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 当属性值更改时发生。
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 获取当前正在执行的任务（如果有）。
    /// </summary>
    public Task? ExecutionTask
    {
        get => this.executionTask;
        private set
        {
            if (ReferenceEquals(this.executionTask, value))
            {
                return;
            }

            this.executionTask = value;
            OnPropertyChanged(nameof(ExecutionTask));
            OnPropertyChanged(nameof(IsRunning));
        }
    }

    /// <summary>
    /// 获取一个值，该值指示当前命令是否可以被取消。
    /// </summary>
    public bool CanBeCanceled => this.IsCancellationSupported && this.IsRunning && !this.IsCancellationRequested;

    /// <summary>
    /// 获取一个值，该值指示当前命令是否已请求取消。
    /// </summary>
    public bool IsCancellationRequested => this.cancellationTokenSource?.IsCancellationRequested == true;

    /// <summary>
    /// 获取一个值，该值指示命令当前是否正在运行。
    /// </summary>
    public bool IsRunning => this.ExecutionTask?.IsCompleted == false;

    /// <summary>
    /// 获取一个值，该值指示当前命令是否支持取消。
    /// </summary>
    public bool IsCancellationSupported { get; }

    /// <summary>
    /// 初始化<see cref="AsyncRelayCommand"/>类的新实例。
    /// </summary>
    /// <param name="execute">要执行的异步委托。</param>
    /// <exception cref="System.ArgumentNullException">如果<paramref name="execute"/>为null，则抛出。</exception>
    public AsyncRelayCommand(Func<Task> execute)
        : this(execute, null, false)
    {
    }

    /// <summary>
    /// 初始化<see cref="AsyncRelayCommand"/>类的新实例。
    /// </summary>
    /// <param name="execute">要执行的异步委托。</param>
    /// <param name="canExecute">用于确定命令是否可以执行的同步谓词。</param>
    /// <exception cref="System.ArgumentNullException">如果<paramref name="execute"/>为null，则抛出。</exception>
    /// <exception cref="System.ArgumentNullException">如果<paramref name="canExecute"/>为null，则抛出。</exception>
    public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute)
        : this(execute, canExecute, false)
    {
    }

    /// <summary>
    /// 初始化<see cref="AsyncRelayCommand"/>类的新实例。
    /// </summary>
    /// <param name="execute">要执行的异步委托。</param>
    /// <param name="canExecute">用于确定命令是否可以执行的同步谓词。</param>
    /// <param name="allowConcurrentExecutions">是否允许并发执行。</param>
    /// <exception cref="System.ArgumentNullException">如果<paramref name="execute"/>为null，则抛出。</exception>
    /// <exception cref="System.ArgumentNullException">如果<paramref name="canExecute"/>为null，则抛出。</exception>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute, bool allowConcurrentExecutions)
    {
        ArgumentNullException.ThrowIfNull(execute);

        // 将不带取消令牌的委托包装为带取消令牌的委托
        this.execute = (cancellationToken) => execute();
        this.canExecute = canExecute;
        this.IsCancellationSupported = !allowConcurrentExecutions;
    }

    /// <summary>
    /// 初始化<see cref="AsyncRelayCommand"/>类的新实例。
    /// </summary>
    /// <param name="cancelableExecute">要执行的可取消异步委托。</param>
    /// <exception cref="System.ArgumentNullException">如果<paramref name="cancelableExecute"/>为null，则抛出。</exception>
    public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute)
        : this(cancelableExecute, null)
    {
    }

    /// <summary>
    /// 初始化<see cref="AsyncRelayCommand"/>类的新实例。
    /// </summary>
    /// <param name="cancelableExecute">要执行的可取消异步委托。</param>
    /// <param name="canExecute">用于确定命令是否可以执行的同步谓词。</param>
    /// <exception cref="System.ArgumentNullException">如果<paramref name="cancelableExecute"/>为null，则抛出。</exception>
    /// <exception cref="System.ArgumentNullException">如果<paramref name="canExecute"/>为null，则抛出。</exception>
    public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute, Func<bool>? canExecute)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);

        this.execute = cancelableExecute;
        this.canExecute = canExecute;
        this.IsCancellationSupported = true;
    }

    /// <summary>
    /// 通知命令的可执行状态可能已更改。
    /// </summary>
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 尝试取消当前正在执行的命令。
    /// </summary>
    public void Cancel()
    {
        if (!this.CanBeCanceled)
        {
            return;
        }

        this.cancellationTokenSource?.Cancel();

        OnPropertyChanged(nameof(IsCancellationRequested));
        OnPropertyChanged(nameof(CanBeCanceled));
    }

    /// <summary>
    /// 确定此<see cref="ICommand"/>是否可以在其当前状态下执行。
    /// </summary>
    /// <param name="parameter">
    /// 命令使用的数据。如果命令不需要传递数据，可以将此对象设置为<see langword="null"/>。
    /// </param>
    /// <returns>如果此命令可以执行，则为<see langword="true"/>，否则为<see langword="false"/>。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanExecute(object? parameter)
    {
        return this.canExecute?.Invoke() != false && (this.IsCancellationSupported || this.ExecutionTask is null || this.ExecutionTask.IsCompleted);
    }

    /// <summary>
    /// 异步执行命令。
    /// </summary>
    /// <param name="parameter">
    /// 命令使用的数据。如果命令不需要传递数据，可以将此对象设置为<see langword="null"/>。
    /// </param>
    /// <returns>表示异步操作的任务。</returns>
    public async Task ExecuteAsync(object? parameter)
    {
        if (!this.CanExecute(parameter))
        {
            return;
        }

        if (this.IsCancellationSupported)
        {
            await ExecuteConcurrentAsync();
        }
        else
        {
            await this.execute(CancellationToken.None);
        }
    }

    /// <summary>
    /// 执行<see cref="ICommand.Execute(object)"/>方法的同步包装。
    /// </summary>
    /// <param name="parameter">
    /// 命令使用的数据。如果命令不需要传递数据，可以将此对象设置为<see langword="null"/>。
    /// </param>
    public void Execute(object? parameter)
    {
        _ = ExecuteAsync(parameter);
    }

    /// <summary>
    /// 执行当前命令。
    /// </summary>
    /// <returns>表示异步操作的任务。</returns>
    private async Task ExecuteConcurrentAsync()
    {
        // 如果已经有一个任务在运行，直接返回
        if (this.ExecutionTask is not null && !this.ExecutionTask.IsCompleted)
        {
            return;
        }

        // 创建一个新的取消令牌源
        this.cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = this.cancellationTokenSource.Token;

        Task executionTask = this.execute(cancellationToken);

        this.ExecutionTask = executionTask;

        OnPropertyChanged(nameof(CanBeCanceled));
        OnPropertyChanged(nameof(IsCancellationRequested));

        try
        {
            await executionTask;
        }
        catch (OperationCanceledException)
        {
            // 如果操作被取消，我们不需要做任何特殊处理
        }
        finally
        {
            this.cancellationTokenSource.Dispose();
            this.cancellationTokenSource = null;

            OnPropertyChanged(nameof(IsCancellationRequested));
            OnPropertyChanged(nameof(CanBeCanceled));
        }
    }

    /// <summary>
    /// 引发<see cref="PropertyChanged"/>事件。
    /// </summary>
    /// <param name="propertyName">已更改的属性的名称。</param>
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}