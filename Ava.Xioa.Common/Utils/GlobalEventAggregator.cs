using Prism.Events;

namespace Ava.Xioa.Common.Utils;

public static class GlobalEventAggregator
{
    public static IEventAggregator? EventAggregator { get; set; }
}