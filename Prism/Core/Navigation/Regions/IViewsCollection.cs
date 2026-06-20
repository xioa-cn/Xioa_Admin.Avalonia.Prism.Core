using System.Collections.Generic;

namespace Prism.Navigation.Regions;

public interface IViewsCollection : IEnumerable<object>
{
    int Count { get; }

    bool Contains(object view);

    int IndexOf(object view);

    void CopyTo(ICollection<object> target);

    object? FirstOrDefault();
}