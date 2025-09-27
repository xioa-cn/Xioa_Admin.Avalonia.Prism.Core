using Prism.Events;

namespace AvaloniaApplication.ViewModels;

public abstract class ViewModelBase : Ava.Xioa.Common.ObservableBindable
{
    public readonly IEventAggregator? _eventAggregator;

    public ViewModelBase()
    {
    }

    public ViewModelBase(IEventAggregator? aggregator)
    {
        _eventAggregator = aggregator;
    }
}