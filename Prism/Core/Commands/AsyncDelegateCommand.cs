using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Prism.Commands;

public sealed class AsyncDelegateCommand : ICommand
{
    private readonly AsyncRelayCommand _command;
    private readonly PropertyObserver _propertyObserver;

    public AsyncDelegateCommand(Func<Task> executeMethod)
        : this(executeMethod, null)
    {
    }

    public AsyncDelegateCommand(Func<Task> executeMethod, Func<bool>? canExecuteMethod)
    {
        _command = canExecuteMethod is null
            ? new AsyncRelayCommand(executeMethod)
            : new AsyncRelayCommand(executeMethod, canExecuteMethod);
        _propertyObserver = new PropertyObserver(RaiseCanExecuteChanged);
    }

    public bool IsRunning => _command.IsRunning;

    public Task? ExecutionTask => _command.ExecutionTask;

    public event EventHandler? CanExecuteChanged
    {
        add => _command.CanExecuteChanged += value;
        remove => _command.CanExecuteChanged -= value;
    }

    public bool CanExecute(object? parameter) => _command.CanExecute(parameter);

    public void Execute(object? parameter) => _command.Execute(parameter);

    public Task Execute() => _command.ExecuteAsync(null);

    public void RaiseCanExecuteChanged() => _command.NotifyCanExecuteChanged();

    public AsyncDelegateCommand ObservesProperty<T>(Expression<Func<T>> propertyExpression)
    {
        _propertyObserver.Observes(propertyExpression);
        return this;
    }

    public AsyncDelegateCommand ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
    {
        ObservesProperty(canExecuteExpression);
        return this;
    }
}

public sealed class AsyncDelegateCommand<T> : ICommand
{
    private readonly AsyncRelayCommand<T?> _command;
    private readonly PropertyObserver _propertyObserver;

    public AsyncDelegateCommand(Func<T?, Task> executeMethod)
        : this(executeMethod, null)
    {
    }

    public AsyncDelegateCommand(Func<T?, Task> executeMethod, Predicate<T?>? canExecuteMethod)
    {
        _command = canExecuteMethod is null
            ? new AsyncRelayCommand<T?>(executeMethod)
            : new AsyncRelayCommand<T?>(executeMethod, canExecuteMethod);
        _propertyObserver = new PropertyObserver(RaiseCanExecuteChanged);
    }

    public bool IsRunning => _command.IsRunning;

    public Task? ExecutionTask => _command.ExecutionTask;

    public event EventHandler? CanExecuteChanged
    {
        add => _command.CanExecuteChanged += value;
        remove => _command.CanExecuteChanged -= value;
    }

    public bool CanExecute(T? parameter) => _command.CanExecute(parameter);

    public bool CanExecute(object? parameter) => _command.CanExecute((T?)parameter);

    public void Execute(T? parameter) => _command.Execute(parameter);

    public void Execute(object? parameter) => _command.Execute((T?)parameter);

    public Task ExecuteAsync(T? parameter) => _command.ExecuteAsync(parameter);

    public void RaiseCanExecuteChanged() => _command.NotifyCanExecuteChanged();

    public AsyncDelegateCommand<T> ObservesProperty<TProperty>(Expression<Func<TProperty>> propertyExpression)
    {
        _propertyObserver.Observes(propertyExpression);
        return this;
    }

    public AsyncDelegateCommand<T> ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
    {
        ObservesProperty(canExecuteExpression);
        return this;
    }
}
