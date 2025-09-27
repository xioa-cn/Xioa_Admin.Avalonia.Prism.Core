using System;
using System.Runtime.CompilerServices;

namespace Ava.Xioa.Common.Input;

public sealed partial class RelayCommand<T> : IRelayCommand<T>
{
    private readonly Action<T?> execute;

    private readonly Predicate<T?>? canExecute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<T?> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);

        this.execute = execute;
    }

    public RelayCommand(Action<T?> execute, Predicate<T?> canExecute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.execute = execute;
        this.canExecute = canExecute;
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanExecute(T? parameter)
    {
        return this.canExecute?.Invoke(parameter) != false;
    }

    public void Execute(T? parameter)
    {
        this.execute(parameter);
    }

    public bool CanExecute(object? parameter)
    {
        if (parameter is null)
        {
            return CanExecute(default);
        }
        
        if (parameter is T typedParameter)
        {
            return CanExecute(typedParameter);
        }
        
        return false;
    }

    public void Execute(object? parameter)
    {
        if (parameter is null)
        {
            Execute(default);
            return;
        }
        
        if (parameter is T typedParameter)
        {
            Execute(typedParameter);
            return;
        }
        
        throw new InvalidOperationException($"Parameter {parameter} cannot be converted to type {typeof(T)}");
    }
}