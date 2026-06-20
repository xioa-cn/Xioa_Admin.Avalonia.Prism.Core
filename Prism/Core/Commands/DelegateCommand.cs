using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Prism.Commands;

public class DelegateCommand : ICommand
{
    private readonly RelayCommand _command;
    private readonly PropertyObserver _propertyObserver;

    public DelegateCommand(Action executeMethod)
        : this(executeMethod, null)
    {
    }

    public DelegateCommand(Action executeMethod, Func<bool>? canExecuteMethod)
    {
        _command = canExecuteMethod is null
            ? new RelayCommand(executeMethod)
            : new RelayCommand(executeMethod, canExecuteMethod);
        _propertyObserver = new PropertyObserver(RaiseCanExecuteChanged);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => _command.CanExecuteChanged += value;
        remove => _command.CanExecuteChanged -= value;
    }

    public bool CanExecute() => _command.CanExecute(null);

    public bool CanExecute(object? parameter) => _command.CanExecute(parameter);

    public void Execute() => _command.Execute(null);

    public void Execute(object? parameter) => _command.Execute(parameter);

    public void RaiseCanExecuteChanged() => _command.NotifyCanExecuteChanged();

    public DelegateCommand ObservesProperty<T>(Expression<Func<T>> propertyExpression)
    {
        _propertyObserver.Observes(propertyExpression);
        return this;
    }

    public DelegateCommand ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
    {
        ObservesProperty(canExecuteExpression);
        return this;
    }

    public static AsyncDelegateCommand FromAsyncHandler(Func<Task> executeMethod)
    {
        return new AsyncDelegateCommand(executeMethod);
    }

    public static AsyncDelegateCommand FromAsyncHandler(Func<Task> executeMethod, Func<bool> canExecuteMethod)
    {
        return new AsyncDelegateCommand(executeMethod, canExecuteMethod);
    }
}

public class DelegateCommand<T> : ICommand
{
    private readonly RelayCommand<T?> _command;
    private readonly PropertyObserver _propertyObserver;

    public DelegateCommand(Action<T?> executeMethod)
        : this(executeMethod, null)
    {
    }

    public DelegateCommand(Action<T?> executeMethod, Predicate<T?>? canExecuteMethod)
    {
        _command = canExecuteMethod is null
            ? new RelayCommand<T?>(executeMethod)
            : new RelayCommand<T?>(executeMethod, canExecuteMethod);
        _propertyObserver = new PropertyObserver(RaiseCanExecuteChanged);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => _command.CanExecuteChanged += value;
        remove => _command.CanExecuteChanged -= value;
    }

    public bool CanExecute(T? parameter) => _command.CanExecute(parameter);

    public bool CanExecute(object? parameter) => _command.CanExecute((T?)parameter);

    public void Execute(T? parameter) => _command.Execute(parameter);

    public void Execute(object? parameter) => _command.Execute((T?)parameter);

    public void RaiseCanExecuteChanged() => _command.NotifyCanExecuteChanged();

    public DelegateCommand<T> ObservesProperty<TProperty>(Expression<Func<TProperty>> propertyExpression)
    {
        _propertyObserver.Observes(propertyExpression);
        return this;
    }

    public DelegateCommand<T> ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
    {
        ObservesProperty(canExecuteExpression);
        return this;
    }

    public static AsyncDelegateCommand<T> FromAsyncHandler(Func<T?, Task> executeMethod)
    {
        return new AsyncDelegateCommand<T>(executeMethod);
    }

    public static AsyncDelegateCommand<T> FromAsyncHandler(Func<T?, Task> executeMethod, Predicate<T?> canExecuteMethod)
    {
        return new AsyncDelegateCommand<T>(executeMethod, canExecuteMethod);
    }
}
