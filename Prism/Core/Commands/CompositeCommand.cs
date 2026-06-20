using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism;

namespace Prism.Commands;

public class CompositeCommand : ICommand
{
    private readonly List<ICommand> _commands = new();
    private readonly bool _monitorCommandActivity;

    public CompositeCommand()
    {
    }

    public CompositeCommand(bool monitorCommandActivity)
    {
        _monitorCommandActivity = monitorCommandActivity;
    }

    public event EventHandler? CanExecuteChanged;

    public virtual void RegisterCommand(ICommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (_commands.Contains(command))
        {
            return;
        }

        _commands.Add(command);
        command.CanExecuteChanged += OnChildCanExecuteChanged;
        if (command is IActiveAware activeAware)
        {
            activeAware.IsActiveChanged += OnChildCanExecuteChanged;
        }

        RaiseCanExecuteChanged();
    }

    public virtual void UnregisterCommand(ICommand command)
    {
        if (_commands.Remove(command))
        {
            command.CanExecuteChanged -= OnChildCanExecuteChanged;
            if (command is IActiveAware activeAware)
            {
                activeAware.IsActiveChanged -= OnChildCanExecuteChanged;
            }

            RaiseCanExecuteChanged();
        }
    }

    public virtual bool CanExecute(object? parameter) => GetExecutableCommands().All(command => command.CanExecute(parameter));

    public virtual void Execute(object? parameter)
    {
        foreach (var command in GetExecutableCommands().Where(command => command.CanExecute(parameter)).ToArray())
        {
            command.Execute(parameter);
        }
    }

    public virtual void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private void OnChildCanExecuteChanged(object? sender, EventArgs e)
    {
        if (!_monitorCommandActivity)
        {
            RaiseCanExecuteChanged();
        }
    }

    private IEnumerable<ICommand> GetExecutableCommands()
    {
        return _commands.Where(command => !_monitorCommandActivity ||
                                          command is not IActiveAware activeAware ||
                                          activeAware.IsActive);
    }
}
