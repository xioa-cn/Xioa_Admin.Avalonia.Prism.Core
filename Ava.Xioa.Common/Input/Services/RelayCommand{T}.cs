using System;
using System.Windows.Input;

namespace Ava.Xioa.Common.Input;

public sealed class RelayCommand<T> : IRelayCommand<T>, IRelayCommand, ICommand
{
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

    public bool CanExecute(T? parameter)
    {
        throw new NotImplementedException();
    }

    public void Execute(T? parameter)
    {
        throw new NotImplementedException();
    }
}