namespace Prism.Navigation.Regions;

public sealed class RegionNavigationJournal : IRegionNavigationJournal
{
    private const int DefaultMaxEntryCount = 100;
    private readonly Region _region;
    private readonly List<IRegionNavigationJournalEntry> _entries = new();
    private readonly object _syncRoot = new();
    private int _currentIndex = -1;
    private int _maxEntryCount = DefaultMaxEntryCount;

    internal RegionNavigationJournal(Region region)
    {
        _region = region;
    }

    public event EventHandler? CurrentEntryChanged;

    public int MaxEntryCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _maxEntryCount;
            }
        }
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Journal max entry count must be greater than zero.");
            }

            lock (_syncRoot)
            {
                _maxEntryCount = value;
                TrimToMaxEntryCount();
            }
        }
    }

    public IRegionNavigationJournalEntry? CurrentEntry
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentIndex >= 0 && _currentIndex < _entries.Count ? _entries[_currentIndex] : null;
            }
        }
    }

    public bool CanGoBack
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentIndex > 0;
            }
        }
    }

    public bool CanGoForward
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentIndex >= 0 && _currentIndex < _entries.Count - 1;
            }
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _entries.Clear();
            _currentIndex = -1;
        }

        CurrentEntryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void GoBack()
    {
        _ = GoBackAsync();
    }

    public void GoForward()
    {
        _ = GoForwardAsync();
    }

    public Task<NavigationResult> GoBackAsync()
    {
        return GoBackAsync(new NavigationParameters());
    }

    public Task<NavigationResult> GoBackAsync(INavigationParameters navigationParameters)
    {
        IRegionNavigationJournalEntry entry;
        lock (_syncRoot)
        {
            if (_currentIndex <= 0)
            {
                return Task.FromResult(new NavigationResult(false));
            }

            entry = _entries[--_currentIndex];
        }

        CurrentEntryChanged?.Invoke(this, EventArgs.Empty);
        return NavigateToJournalEntryAsync(entry, navigationParameters);
    }

    public Task<NavigationResult> GoForwardAsync()
    {
        return GoForwardAsync(new NavigationParameters());
    }

    public Task<NavigationResult> GoForwardAsync(INavigationParameters navigationParameters)
    {
        IRegionNavigationJournalEntry entry;
        lock (_syncRoot)
        {
            if (_currentIndex < 0 || _currentIndex >= _entries.Count - 1)
            {
                return Task.FromResult(new NavigationResult(false));
            }

            entry = _entries[++_currentIndex];
        }

        CurrentEntryChanged?.Invoke(this, EventArgs.Empty);
        return NavigateToJournalEntryAsync(entry, navigationParameters);
    }

    internal void Record(NavigationContext navigationContext)
    {
        lock (_syncRoot)
        {
            if (_currentIndex >= 0 &&
                _currentIndex < _entries.Count &&
                string.Equals(_entries[_currentIndex].Uri.OriginalString, navigationContext.Uri.OriginalString, StringComparison.Ordinal))
            {
                return;
            }

            if (_currentIndex < _entries.Count - 1)
            {
                _entries.RemoveRange(_currentIndex + 1, _entries.Count - _currentIndex - 1);
            }

            _entries.Add(new RegionNavigationJournalEntry(
                navigationContext.RegionName,
                navigationContext.Uri,
                CloneParameters(navigationContext.Parameters)));
            _currentIndex = _entries.Count - 1;
            TrimToMaxEntryCount();
        }

        CurrentEntryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TrimToMaxEntryCount()
    {
        if (_entries.Count <= _maxEntryCount)
        {
            return;
        }

        var removeCount = _entries.Count - _maxEntryCount;
        _entries.RemoveRange(0, removeCount);
        _currentIndex = Math.Max(-1, _currentIndex - removeCount);
    }

    private Task<NavigationResult> NavigateToJournalEntryAsync(IRegionNavigationJournalEntry entry, INavigationParameters navigationParameters)
    {
        if (_region.RegionManager is not RegionManager regionManager)
        {
            return Task.FromResult(new NavigationResult(new InvalidOperationException($"Region '{_region.Name}' is not attached to a RegionManager.")));
        }

        var mergedParameters = CloneParameters(entry.Parameters);
        foreach (var parameter in navigationParameters)
        {
            mergedParameters[parameter.Key] = parameter.Value;
        }

        return regionManager.NavigateFromJournalAsync(entry.RegionName, entry.Uri.OriginalString, mergedParameters);
    }

    private static NavigationParameters CloneParameters(INavigationParameters source)
    {
        if (source.Clone() is NavigationParameters clonedParameters)
        {
            return clonedParameters;
        }

        var clone = new NavigationParameters();
        foreach (var parameter in source)
        {
            clone.Add(parameter.Key, parameter.Value);
        }

        return clone;
    }
}
