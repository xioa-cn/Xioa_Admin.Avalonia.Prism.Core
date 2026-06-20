using System.Collections.Generic;

namespace Prism.Navigation.Regions;

public interface IRegionCollection : IEnumerable<IRegion>
{
    IRegion this[string regionName] { get; }

    bool ContainsRegionWithName(string regionName);

    void Add(IRegion region);

    bool Remove(string regionName);

    void CopyTo(ICollection<IRegion> target);
}