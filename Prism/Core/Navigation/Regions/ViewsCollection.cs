using System.Collections.Generic;

namespace Prism.Navigation.Regions;

public sealed class ViewsCollection : IViewsCollection
{
    private readonly List<object> _views = new();
    private readonly object _syncRoot = new();

    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _views.Count;
            }
        }
    }

    public bool Contains(object view)
    {
        lock (_syncRoot)
        {
            return _views.Contains(view);
        }
    }

    public bool Add(object view)
    {
        lock (_syncRoot)
        {
            if (!_views.Contains(view))
            {
                _views.Add(view);
                return true;
            }

            return false;
        }
    }

    public bool Remove(object view)
    {
        lock (_syncRoot)
        {
            return _views.Remove(view);
        }
    }

    public int IndexOf(object view)
    {
        lock (_syncRoot)
        {
            return _views.IndexOf(view);
        }
    }

    public void CopyTo(ICollection<object> target)
    {
        lock (_syncRoot)
        {
            foreach (var view in _views)
            {
                target.Add(view);
            }
        }
    }

    public object? FirstOrDefault()
    {
        lock (_syncRoot)
        {
            return _views.Count > 0 ? _views[0] : null;
        }
    }

    public IEnumerator<object> GetEnumerator()
    {
        var snapshot = new List<object>();
        CopyTo(snapshot);
        return snapshot.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}