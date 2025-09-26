using System;

namespace Ava.Xioa.Common.Input;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RelayCommandAttribute : Attribute
{
    public string? CanExecute { get; init; }
    public bool AllowConcurrentExecutions { get; init; }
    public bool FlowExceptionsToTaskScheduler { get; init; }
    public bool IncludeCancelCommand { get; init; }
}