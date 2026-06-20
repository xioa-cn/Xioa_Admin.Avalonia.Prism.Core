namespace Prism.Navigation.Regions;

internal sealed class AwareTargetCacheEntry
{
    private readonly object _syncRoot = new();
    private object? _dataContext;
    private object[] _targets = Array.Empty<object>();
    private bool _initialized;

    public IReadOnlyList<object> GetTargets(object view, object? dataContext)
    {
        lock (_syncRoot)
        {
            if (_initialized && ReferenceEquals(_dataContext, dataContext))
            {
                return _targets;
            }

            _dataContext = dataContext;
            _initialized = true;
            _targets = CreateTargets(view, dataContext);
            return _targets;
        }
    }

    private static object[] CreateTargets(object view, object? dataContext)
    {
        if (dataContext is null || ReferenceEquals(view, dataContext))
        {
            return new[] { view };
        }

        return new[] { view, dataContext };
    }
}