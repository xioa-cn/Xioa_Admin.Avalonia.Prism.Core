using System;
using Prism.Commands;

namespace Ava.Xioa.Common.Input;

public sealed partial class RelayCommand : DelegateCommand
{
    public RelayCommand(Action executeMethod) : base(executeMethod)
    {
    }

    public RelayCommand(Action executeMethod, Func<bool> canExecuteMethod) : base(executeMethod, canExecuteMethod)
    {
    }
}

// public sealed partial class RelayCommand : IRelayCommand
// {
//     private readonly Action execute;
//     
//     private readonly Func<bool>? canExecute;
//
//     private bool isRunning;
//     
//     public bool IsRunning => isRunning;
//     public event EventHandler? CanExecuteChanged;
//
//     public RelayCommand(Action execute)
//     {
//         ArgumentNullException.ThrowIfNull(execute);
//
//         this.execute = execute;
//     }
//
//     public RelayCommand(Action execute, Func<bool> canExecute)
//     {
//         ArgumentNullException.ThrowIfNull(execute);
//         ArgumentNullException.ThrowIfNull(canExecute);
//
//         this.execute = execute;
//         this.canExecute = canExecute;
//     }
//
//     public void NotifyCanExecuteChanged()
//     {
//         isRunning = true;
//         CanExecuteChanged?.Invoke(this, EventArgs.Empty);
//         isRunning = false;
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public bool CanExecute(object? parameter)
//     {
//         return this.canExecute?.Invoke() != false;
//     }
//
//     public void Execute(object? parameter)
//     {
//         this.execute();
//     }
// }