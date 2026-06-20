using System;
using System.Threading.Tasks;

namespace Prism.Events;

public sealed class ThreadPoolEventBackgroundDispatcher : IEventBackgroundDispatcher
{
    public void Dispatch(Action action)
    {
        Task.Run(action);
    }
}
