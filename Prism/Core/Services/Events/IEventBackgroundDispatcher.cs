using System;

namespace Prism.Events;

public interface IEventBackgroundDispatcher
{
    void Dispatch(Action action);
}