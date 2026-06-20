namespace Prism.Navigation.Regions;

public sealed class RegionCollection : IRegionCollection
{
    private readonly Dictionary<string, IRegion> _regions = new(StringComparer.Ordinal);
    private readonly object _syncRoot = new();

    public IRegion this[string regionName]
    {
        get
        {
            lock (_syncRoot)
            {
                return _regions[regionName];
            }
        }
    }

    public bool ContainsRegionWithName(string regionName)
    {
        lock (_syncRoot)
        {
            return _regions.ContainsKey(regionName);
        }
    }

    public void Add(IRegion region)
    {
        lock (_syncRoot)
        {
            _regions[region.Name] = region;
        }
    }

    public bool Remove(string regionName)
    {
        lock (_syncRoot)
        {
            return _regions.Remove(regionName);
        }
    }

    public void CopyTo(ICollection<IRegion> target)
    {
        lock (_syncRoot)
        {
            foreach (var region in _regions.Values)
            {
                target.Add(region);
            }
        }
    }

    public IEnumerator<IRegion> GetEnumerator()
    {
        var snapshot = new List<IRegion>();
        CopyTo(snapshot);
        return snapshot.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}