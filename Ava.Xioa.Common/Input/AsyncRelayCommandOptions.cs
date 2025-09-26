using System;

namespace Ava.Xioa.Common.Input;

[Flags]
public enum AsyncRelayCommandOptions
{
    None = 0,

    AllowConcurrentExecutions = 1 << 0,

    FlowExceptionsToTaskScheduler = 1 << 1
}