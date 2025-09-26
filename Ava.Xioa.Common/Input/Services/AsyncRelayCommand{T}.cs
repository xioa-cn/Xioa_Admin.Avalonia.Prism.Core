using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Ava.Xioa.Common.Input;

public sealed partial class AsyncRelayCommand<T> : IAsyncRelayCommand<T>, ICancellationAwareCommand
{
    public async Task ExecuteAsync(T? parameter)
    {
        throw new NotImplementedException();
    }

    public bool CanExecute(object? parameter)
    {
        throw new NotImplementedException();
    }

    public void Execute(object? parameter)
    {
        throw new NotImplementedException();
    }

    public event EventHandler? CanExecuteChanged;
    public void NotifyCanExecuteChanged()
    {
        throw new NotImplementedException();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public Task? ExecutionTask { get; }
    public bool CanBeCanceled { get; }
    public bool IsCancellationRequested { get; }
    public bool IsRunning { get; }
    public async Task ExecuteAsync(object? parameter)
    {
        throw new NotImplementedException();
    }

    public void Cancel()
    {
        throw new NotImplementedException();
    }

    public bool CanExecute(T? parameter)
    {
        throw new NotImplementedException();
    }

    public void Execute(T? parameter)
    {
        throw new NotImplementedException();
    }

    public bool IsCancellationSupported { get; }
}