using System;
using System.Runtime.CompilerServices;

namespace Ava.Xioa.Common.Input;

public sealed partial class RelayCommand : IRelayCommand
{
    private readonly Action execute;
    
    private readonly Func<bool>? canExecute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action execute)
    {
        ArgumentNullException.ThrowIfNull(execute);

        this.execute = execute;
    }

    public RelayCommand(Action execute, Func<bool> canExecute)
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
    public bool CanExecute(object? parameter)
    {
        return this.canExecute?.Invoke() != false;
    }

    public void Execute(object? parameter)
    {
        this.execute();
    }
}